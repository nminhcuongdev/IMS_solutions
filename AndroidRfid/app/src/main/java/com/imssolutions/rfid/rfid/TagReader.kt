package com.imssolutions.rfid.rfid

import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

data class TagRead(val epc: String, val rssi: Double)

enum class ReaderState { Unavailable, Connecting, Waiting, Ready, Detached }

enum class ReaderKind(val label: String) {
    CipherLab("CipherLab RS36"),
    DensoSp1("DENSO SP1"),
}

interface TagReader {
    val kind: ReaderKind
    val powerRange: IntRange
    val state: StateFlow<ReaderState>
    val reads: SharedFlow<TagRead>

    fun start()

    fun stop()

    fun setPower(value: Int): String?

    fun trigger(on: Boolean): String?

    fun release() = stop()
}
