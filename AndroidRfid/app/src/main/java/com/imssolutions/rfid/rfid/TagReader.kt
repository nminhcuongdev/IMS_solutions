package com.imssolutions.rfid.rfid

import kotlinx.coroutines.flow.SharedFlow
import kotlinx.coroutines.flow.StateFlow

data class TagRead(val epc: String, val rssi: Double)

enum class ReaderState { Unavailable, Connecting, Waiting, Ready, Detached }

enum class ReaderKind(val label: String) {
    CipherLab("CipherLab RS36"),
    DensoSp1("DENSO SP1"),
    NewlandMt93("Newland MT93"),
}

enum class ReadMode { Rfid, Barcode, Both }

data class ReaderCaps(
    val modes: List<ReadMode>,
    val barcodeBySoftTrigger: Boolean,
    val barcodeByKeyboard: Boolean,
    val beepOnRead: Boolean = false,
)

interface TagReader {
    val kind: ReaderKind
    val powerRange: IntRange
    val caps: ReaderCaps
    val state: StateFlow<ReaderState>
    val reads: SharedFlow<TagRead>
    val barcodes: SharedFlow<String>

    fun start()

    fun stop()

    suspend fun setPower(value: Int): String?

    suspend fun setMode(mode: ReadMode): String?

    fun trigger(on: Boolean): String?

    fun release() = stop()
}
