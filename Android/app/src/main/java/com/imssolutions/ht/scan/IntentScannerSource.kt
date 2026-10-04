package com.imssolutions.ht.scan

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import android.content.IntentFilter
import androidx.core.content.ContextCompat

class IntentScannerSource(
    private val context: Context,
    private val action: String = DEFAULT_ACTION,
    private val extraKey: String = DEFAULT_EXTRA,
) : ScannerSource {

    override fun observe(onScan: (String) -> Unit): () -> Unit {
        val receiver = object : BroadcastReceiver() {
            override fun onReceive(ctx: Context?, intent: Intent?) {
                val code = intent?.getStringExtra(extraKey)?.trim().orEmpty()
                if (code.isNotEmpty()) onScan(code)
            }
        }
        ContextCompat.registerReceiver(
            context, receiver, IntentFilter(action), ContextCompat.RECEIVER_EXPORTED,
        )
        return {
            runCatching { context.unregisterReceiver(receiver) }
        }
    }

    companion object {
        const val DEFAULT_ACTION = "nlscan.action.SCANNER_RESULT"
        const val DEFAULT_EXTRA = "SCAN_BARCODE1"
    }
}
