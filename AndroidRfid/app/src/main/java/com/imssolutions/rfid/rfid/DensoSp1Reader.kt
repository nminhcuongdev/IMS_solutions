package com.imssolutions.rfid.rfid

import com.densowave.scannersdk.Common.CommManager
import com.densowave.scannersdk.Common.CommScanner
import com.densowave.scannersdk.Common.CommStatusChangedEvent
import com.densowave.scannersdk.Const.CommConst
import com.densowave.scannersdk.Dto.RFIDScannerSettings
import com.densowave.scannersdk.Listener.RFIDDataDelegate
import com.densowave.scannersdk.Listener.ScannerAcceptStatusListener
import com.densowave.scannersdk.Listener.ScannerStatusListener
import com.densowave.scannersdk.RFID.RFIDDataReceivedEvent
import com.densowave.scannersdk.RFID.RFIDScanner
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import java.util.concurrent.Executors

class DensoSp1Reader(initialPower: Int) : TagReader {

    override val kind = ReaderKind.DensoSp1
    override val powerRange = MIN_POWER..MAX_POWER

    private val worker = Executors.newSingleThreadExecutor()

    @Volatile private var scanner: CommScanner? = null
    @Volatile private var rfid: RFIDScanner? = null
    @Volatile private var accepting = false
    @Volatile private var inventoryOpen = false
    @Volatile private var power = initialPower.coerceIn(MIN_POWER, MAX_POWER)

    private val _state = MutableStateFlow(ReaderState.Waiting)
    override val state: StateFlow<ReaderState> = _state.asStateFlow()

    private val _reads = MutableSharedFlow<TagRead>(
        extraBufferCapacity = 1024,
        onBufferOverflow = BufferOverflow.DROP_OLDEST,
    )
    override val reads: SharedFlow<TagRead> = _reads.asSharedFlow()

    private val acceptListener = ScannerAcceptStatusListener { appeared ->
        worker.execute { adopt(appeared) }
    }

    private val statusListener = ScannerStatusListener { source, event: CommStatusChangedEvent ->
        if (source === scanner && event.status == CommConst.ScannerStatus.CLOSE_WAIT) {
            worker.execute {
                dropScanner()
                _state.value = ReaderState.Detached
                beginAccept()
            }
        }
    }

    private val dataDelegate = RFIDDataDelegate { _, event: RFIDDataReceivedEvent ->
        val items = runCatching { event.rfidData }.getOrNull() ?: return@RFIDDataDelegate
        for (d in items) {
            val uii = runCatching { d.uii }.getOrNull() ?: continue
            if (uii.isEmpty()) continue
            val rssi = runCatching { d.rssi.toShort() / 10.0 }.getOrDefault(0.0)
            _reads.tryEmit(TagRead(toHex(uii), rssi))
        }
    }

    override fun start() {
        worker.execute {
            val current = scanner
            if (current != null) {
                openInventory()
                return@execute
            }
            val known = runCatching { CommManager.getScanners() }.getOrNull()?.firstOrNull()
            if (known != null) adopt(known) else beginAccept()
        }
    }

    override fun stop() {
        worker.execute {
            runCatching { rfid?.pressSoftwareTrigger(false) }
            closeInventory()
        }
    }

    override fun release() {
        worker.execute {
            endAccept()
            dropScanner()
            _state.value = ReaderState.Waiting
        }
        worker.shutdown()
    }

    override fun setPower(value: Int): String? {
        val next = value.coerceIn(MIN_POWER, MAX_POWER)
        if (next == power && inventoryOpen) return null
        power = next
        if (rfid == null) return null
        worker.execute {
            closeInventory()
            openInventory()
        }
        return null
    }

    override fun trigger(on: Boolean): String? {
        val r = rfid ?: return NOT_CONNECTED
        if (!inventoryOpen) return NOT_CONNECTED
        return runCatching { r.pressSoftwareTrigger(on) }.exceptionOrNull()?.let(::describe)
    }

    private fun beginAccept() {
        if (accepting) return
        runCatching {
            CommManager.addAcceptStatusListener(acceptListener)
            CommManager.startAccept()
            accepting = true
            _state.value = ReaderState.Waiting
        }.onFailure { _state.value = ReaderState.Unavailable }
    }

    private fun endAccept() {
        if (!accepting) return
        runCatching { CommManager.endAccept() }
        runCatching { CommManager.removeAcceptStatusListener(acceptListener) }
        accepting = false
    }

    private fun adopt(candidate: CommScanner) {
        if (scanner != null) return
        _state.value = ReaderState.Connecting
        val claimed = runCatching { candidate.claim() }.isSuccess
        endAccept()
        if (!claimed) {
            _state.value = ReaderState.Detached
            beginAccept()
            return
        }
        scanner = candidate
        runCatching { candidate.addStatusListener(statusListener) }
        rfid = runCatching { candidate.rfidScanner }.getOrNull()
        if (rfid == null) {
            _state.value = ReaderState.Unavailable
            return
        }
        runCatching { rfid?.setDataDelegate(dataDelegate) }
        openInventory()
    }

    private fun openInventory() {
        val r = rfid ?: return
        if (inventoryOpen) return
        val ok = runCatching {
            val settings: RFIDScannerSettings = r.settings
            settings.scan.triggerMode = RFIDScannerSettings.Scan.TriggerMode.MOMENTARY
            settings.scan.doubleReading = RFIDScannerSettings.Scan.DoubleReading.Free
            settings.scan.powerLevelRead = power
            r.setSettings(settings)
            r.clearDoubleReadingBuffer()
            r.openInventory()
        }.isSuccess
        inventoryOpen = ok
        _state.value = if (ok) ReaderState.Ready else ReaderState.Detached
    }

    private fun closeInventory() {
        if (!inventoryOpen) return
        runCatching { rfid?.close() }
        inventoryOpen = false
    }

    private fun dropScanner() {
        closeInventory()
        val s = scanner ?: return
        runCatching { rfid?.setDataDelegate(null) }
        runCatching { s.removeStatusListener(statusListener) }
        runCatching { s.close() }
        rfid = null
        scanner = null
    }

    private fun describe(e: Throwable): String = e.message ?: e.javaClass.simpleName

    private fun toHex(bytes: ByteArray): String {
        val out = StringBuilder(bytes.size * 2)
        for (b in bytes) {
            val v = b.toInt() and 0xFF
            out.append(HEX[v ushr 4]).append(HEX[v and 0x0F])
        }
        return out.toString()
    }

    companion object {
        const val MIN_POWER = 4
        const val MAX_POWER = 30
        const val NOT_CONNECTED = "SP1 is not connected"
        private const val HEX = "0123456789ABCDEF"
    }
}
