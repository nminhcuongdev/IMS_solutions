package com.imssolutions.rfid.rfid

import com.nlscan.android.uhf.TagInfo
import com.nlscan.android.uhf.UHFManager
import com.nlscan.android.uhf.UHFReader
import kotlinx.coroutines.asCoroutineDispatcher
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.withContext
import org.json.JSONArray
import org.json.JSONObject
import java.util.concurrent.Executors
import java.util.concurrent.TimeUnit

class NewlandMt93Reader(initialPower: Int) : TagReader {

    override val kind = ReaderKind.NewlandMt93
    override val powerRange = MIN_POWER..MAX_POWER
    override val caps = ReaderCaps(
        modes = listOf(ReadMode.Rfid, ReadMode.Barcode),
        barcodeBySoftTrigger = false,
        barcodeByKeyboard = true,
        beepOnRead = true,
    )

    private val worker = Executors.newSingleThreadScheduledExecutor()
    private val dispatcher = worker.asCoroutineDispatcher()

    @Volatile private var manager: UHFManager? = null
    @Volatile private var listening = false
    @Volatile private var connectRequested = false
    @Volatile private var mode = ReadMode.Rfid
    @Volatile private var power = initialPower.coerceIn(MIN_POWER, MAX_POWER)
    @Volatile private var checks = 0

    private val _state = MutableStateFlow(ReaderState.Connecting)
    override val state: StateFlow<ReaderState> = _state.asStateFlow()

    private val _reads = MutableSharedFlow<TagRead>(
        extraBufferCapacity = 1024,
        onBufferOverflow = BufferOverflow.DROP_OLDEST,
    )
    override val reads: SharedFlow<TagRead> = _reads.asSharedFlow()

    override val barcodes: SharedFlow<String> = MutableSharedFlow<String>().asSharedFlow()

    private val tagListener = object : UHFManager.AbsUHFTagInventoryListener() {
        override fun onReadingResult(tags: Array<out TagInfo>?) {
            if (mode == ReadMode.Barcode) return
            tags?.forEach { tag ->
                val epc = tag.EpcId?.takeIf { it.isNotEmpty() }?.let(::toHex) ?: return@forEach
                _reads.tryEmit(TagRead(epc, tag.RSSI.toDouble()))
            }
        }
    }

    override fun start() {
        worker.execute {
            val m = manager ?: runCatching { UHFManager.getInstance() }.getOrNull()
            if (m == null) {
                _state.value = ReaderState.Unavailable
                return@execute
            }
            manager = m
            if (!listening) {
                listening = runCatching { m.registerTagInventoryListener(tagListener) }.isSuccess
            }
            if (connected(m)) {
                becomeReady(m)
            } else {
                _state.value = ReaderState.Connecting
                checks = 0
                connectRequested = false
                worker.schedule({ pollConnection() }, POLL_MS, TimeUnit.MILLISECONDS)
            }
        }
    }

    override fun stop() {
        worker.execute {
            val m = manager ?: return@execute
            runCatching { if (m.isInInventory) m.stopTagInventory() }
            if (listening) {
                runCatching { m.unRegisterTagInventoryListener(tagListener) }
                listening = false
            }
            _state.value = ReaderState.Connecting
        }
    }

    override fun release() {
        stop()
        worker.execute { manager = null }
        worker.shutdown()
    }

    override suspend fun setPower(value: Int): String? = withContext(dispatcher) {
        power = value.coerceIn(MIN_POWER, MAX_POWER)
        val m = manager ?: return@withContext NOT_CONNECTED
        if (!connected(m)) return@withContext NOT_CONNECTED
        applyPower(m)
    }

    override suspend fun setMode(mode: ReadMode): String? = withContext(dispatcher) {
        if (mode !in caps.modes) return@withContext UNSUPPORTED
        this@NewlandMt93Reader.mode = mode
        val m = manager ?: return@withContext NOT_CONNECTED
        if (mode == ReadMode.Barcode) runCatching { if (m.isInInventory) m.stopTagInventory() }
        null
    }

    override fun trigger(on: Boolean): String? {
        if (mode == ReadMode.Barcode) return USE_SCAN_KEY
        val m = manager ?: return NOT_CONNECTED
        return runCatching {
            if (on) {
                if (!connected(m)) return NOT_CONNECTED
                if (m.isInInventory) null else describe(m.startTagInventory())
            } else {
                if (m.isInInventory) describe(m.stopTagInventory()) else null
            }
        }.getOrElse { it.message ?: it.javaClass.simpleName }
    }

    private fun connected(m: UHFManager): Boolean = runCatching { m.isConnect }.getOrDefault(false)

    private fun requestConnect(m: UHFManager) {
        connectRequested = true
        val info = runCatching { m.uhfModuleInfo ?: m.loadUHFModule() }.getOrNull()
        val path = info?.serial_path?.takeIf { it.isNotBlank() }
        val plugin = info?.connectType?.takeIf { it > 0 } ?: UHFManager.PLUGIN_TYPE_SERIEL
        val model = info?.moduleType?.takeIf { it.isNotBlank() }
        runCatching { m.asyncConnect(path, plugin, model, null) }
    }

    private fun pollConnection() {
        val m = manager ?: return
        if (_state.value == ReaderState.Ready) return
        when {
            connected(m) -> becomeReady(m)
            ++checks < MAX_POLLS -> {
                if (!connectRequested && checks >= SERVICE_GRACE_POLLS) requestConnect(m)
                worker.schedule({ pollConnection() }, POLL_MS, TimeUnit.MILLISECONDS)
            }
            else -> _state.value = ReaderState.Detached
        }
    }

    private fun becomeReady(m: UHFManager) {
        if (_state.value == ReaderState.Ready) return
        applyReadProfile(m)
        applyPower(m)
        runCatching { m.setPromptSoundEnable(true) }
        _state.value = ReaderState.Ready
    }

    private fun applyReadProfile(m: UHFManager) {
        READ_PROFILE.forEach { (key, name, value) -> runCatching { m.setParam(key, name, value) } }
    }

    private fun applyPower(m: UHFManager): String? = runCatching {
        val antennas = antennas(m)
        val reported = antennas?.optJSONObject(0)?.optInt("readPower") ?: 0
        val scale = if (reported > CENTI_THRESHOLD) CENTI else 1
        val target = power * scale
        val next = antennas ?: JSONArray().put(JSONObject().put("antid", 1).put("writePower", target))
        for (i in 0 until next.length()) next.getJSONObject(i).put("readPower", target)
        val err = describe(m.setParam(POWER_KEY, POWER_NAME, next.toString()))
        err ?: if (readPower(m) == power) null else APPLY_FAILED
    }.getOrElse { it.message ?: it.javaClass.simpleName }

    private fun antennas(m: UHFManager): JSONArray? =
        m.getParam(POWER_KEY, POWER_NAME, null)?.takeIf { it.isNotBlank() }
            ?.let { JSONArray(it) }?.takeIf { it.length() > 0 }

    private fun readPower(m: UHFManager): Int? = runCatching {
        val value = antennas(m)?.optJSONObject(0)?.optInt("readPower") ?: return null
        if (value > CENTI_THRESHOLD) value / CENTI else value
    }.getOrNull()

    private fun describe(result: UHFReader.READER_STATE?): String? = when (result) {
        UHFReader.READER_STATE.OK_ERR -> null
        null -> APPLY_FAILED
        else -> "MT93 ${result.name}"
    }

    private fun toHex(bytes: ByteArray): String {
        val out = CharArray(bytes.size * 2)
        bytes.forEachIndexed { i, b ->
            val v = b.toInt() and 0xFF
            out[i * 2] = HEX[v ushr 4]
            out[i * 2 + 1] = HEX[v and 0x0F]
        }
        return String(out)
    }

    companion object {
        const val MIN_POWER = 5
        const val MAX_POWER = 30
        const val NOT_CONNECTED = "MT93 UHF module is not connected"
        const val APPLY_FAILED = "MT93 did not accept the setting"
        const val UNSUPPORTED = "Mode not supported on MT93"
        const val USE_SCAN_KEY = "Press the scan key on the MT93 to read a barcode"
        private const val POWER_KEY = "RF_ANTPOWER"
        private const val POWER_NAME = "PARAM_RF_ANTPOWER"
        private const val CENTI = 100
        private const val CENTI_THRESHOLD = 100
        private const val SERVICE_GRACE_POLLS = 6
        private const val POLL_MS = 500L
        private const val MAX_POLLS = 20
        private const val HEX = "0123456789ABCDEF"
        private val READ_PROFILE = listOf(
            Triple("POTL_GEN2_SESSION", "PARAM_POTL_GEN2_SESSION", "0"),
            Triple("POTL_GEN2_TARGET", "PARAM_POTL_GEN2_TARGET", "0"),
            Triple("POTL_GEN2_Q", "PARAM_POTL_GEN2_Q", "-1"),
            Triple("INV_INTERVAL", "PARAM_INV_INTERVAL_TIME", "0"),
        )
    }
}
