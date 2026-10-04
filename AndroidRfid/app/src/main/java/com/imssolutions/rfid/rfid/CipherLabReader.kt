package com.imssolutions.rfid.rfid

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import android.os.Handler
import android.os.Looper
import androidx.core.content.ContextCompat
import com.cipherlab.rfid.ClResult
import com.cipherlab.rfid.GeneralString
import com.cipherlab.rfidapi.RfidManager
import kotlinx.coroutines.channels.BufferOverflow
import kotlinx.coroutines.flow.MutableSharedFlow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asSharedFlow
import kotlinx.coroutines.flow.asStateFlow

class CipherLabReader(context: Context) : TagReader {

    override val kind = ReaderKind.CipherLab
    override val powerRange = MIN_POWER..MAX_POWER

    private val appContext = context.applicationContext
    private var manager: RfidManager? = null
    private var registered = false
    private val handler = Handler(Looper.getMainLooper())
    private var polls = 0

    private val poll = object : Runnable {
        override fun run() {
            if (manager == null) return
            val attached = runCatching { manager?.GetConnectionStatus() == true }.getOrDefault(false)
            when {
                attached -> _state.value = ReaderState.Ready
                ++polls < MAX_POLLS -> handler.postDelayed(this, POLL_MS)
                else -> _state.value = ReaderState.Detached
            }
        }
    }

    private val _state = MutableStateFlow(ReaderState.Connecting)
    override val state: StateFlow<ReaderState> = _state.asStateFlow()

    private val _reads = MutableSharedFlow<TagRead>(
        extraBufferCapacity = 512,
        onBufferOverflow = BufferOverflow.DROP_OLDEST,
    )
    override val reads: SharedFlow<TagRead> = _reads.asSharedFlow()

    private val receiver = object : BroadcastReceiver() {
        override fun onReceive(ctx: Context?, intent: Intent?) {
            when (intent?.action) {
                GeneralString.Intent_RFIDSERVICE_CONNECTED -> refreshState()
                GeneralString.Intent_GUN_Attached -> _state.value = ReaderState.Ready
                GeneralString.Intent_GUN_Unattached -> _state.value = ReaderState.Detached
                GeneralString.Intent_RFIDSERVICE_TAG_DATA -> onTag(intent)
            }
        }
    }

    override fun start() {
        if (!registered) {
            val filter = IntentFilter().apply {
                addAction(GeneralString.Intent_RFIDSERVICE_CONNECTED)
                addAction(GeneralString.Intent_RFIDSERVICE_TAG_DATA)
                addAction(GeneralString.Intent_GUN_Attached)
                addAction(GeneralString.Intent_GUN_Unattached)
            }
            ContextCompat.registerReceiver(appContext, receiver, filter, ContextCompat.RECEIVER_EXPORTED)
            registered = true
        }
        if (manager == null) {
            manager = runCatching { RfidManager.InitInstance(appContext) }.getOrNull()
            _state.value = if (manager == null) ReaderState.Unavailable else ReaderState.Connecting
        }
        if (manager != null && _state.value != ReaderState.Ready) {
            polls = 0
            handler.removeCallbacks(poll)
            handler.postDelayed(poll, POLL_MS)
        }
    }

    override fun stop() {
        handler.removeCallbacks(poll)
        runCatching { manager?.SoftScanTrigger(false) }
        runCatching { manager?.Release() }
        manager = null
        if (registered) {
            runCatching { appContext.unregisterReceiver(receiver) }
            registered = false
        }
        _state.value = ReaderState.Connecting
    }

    private fun refreshState() {
        val attached = runCatching { manager?.GetConnectionStatus() == true }.getOrDefault(false)
        _state.value = if (attached) ReaderState.Ready else ReaderState.Detached
    }

    private fun onTag(intent: Intent) {
        val response = intent.getIntExtra(GeneralString.EXTRA_RESPONSE, RESPONSE_SUCCESS)
        if (response != RESPONSE_SUCCESS) return
        val epc = intent.getStringExtra(GeneralString.EXTRA_EPC)?.trim().orEmpty()
        if (epc.isEmpty()) return
        val rssi = intent.getDoubleExtra(GeneralString.EXTRA_DATA_RSSI, 0.0)
        _reads.tryEmit(TagRead(epc, rssi))
    }

    fun power(): Int? = runCatching { manager?.GetTxPower() }.getOrNull()?.takeIf { it in MIN_POWER..MAX_POWER }

    override fun setPower(value: Int): String? = result { it.SetTxPower(value.coerceIn(MIN_POWER, MAX_POWER)) }

    override fun trigger(on: Boolean): String? = result { it.SoftScanTrigger(on) }

    private inline fun result(block: (RfidManager) -> Int): String? {
        val m = manager ?: return NOT_AVAILABLE
        return runCatching {
            val code = block(m)
            if (code == ClResult.S_OK.ordinal) null else (m.GetLastError() ?: "ERR $code")
        }.getOrElse { it.message ?: it.javaClass.simpleName }
    }

    companion object {
        const val MIN_POWER = 5
        const val MAX_POWER = 30
        const val NOT_AVAILABLE = "RFID service not available"
        private const val RESPONSE_SUCCESS = 0
        private const val POLL_MS = 500L
        private const val MAX_POLLS = 20
    }
}
