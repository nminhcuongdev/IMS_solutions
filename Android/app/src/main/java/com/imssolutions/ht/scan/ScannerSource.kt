package com.imssolutions.ht.scan

import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.rememberUpdatedState

interface ScannerSource {
    fun observe(onScan: (String) -> Unit): () -> Unit

    companion object {
        @Volatile
        var active: ScannerSource = KeyboardWedgeSource
    }
}

object KeyboardWedgeSource : ScannerSource {
    override fun observe(onScan: (String) -> Unit): () -> Unit = { }
}

@Composable
fun OnScan(source: ScannerSource = ScannerSource.active, onScan: (String) -> Unit) {
    val current by rememberUpdatedState(onScan)
    DisposableEffect(source) {
        val cancel = source.observe { if (!ScanGate.blocked) current(it) }
        onDispose { cancel() }
    }
}

object ScanGate {
    private val depth = java.util.concurrent.atomic.AtomicInteger(0)

    val blocked: Boolean get() = depth.get() > 0

    fun block() {
        depth.incrementAndGet()
    }

    fun unblock() {
        if (depth.decrementAndGet() < 0) depth.set(0)
    }
}
