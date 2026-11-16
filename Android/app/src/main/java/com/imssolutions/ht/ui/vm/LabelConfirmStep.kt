package com.imssolutions.ht.ui.vm

sealed interface LabelConfirmStep {
    data class Refuse(val code: String, val message: String) : LabelConfirmStep
    data class Go(val print: Boolean) : LabelConfirmStep
}

fun labelConfirmStep(qty: Double?, labelIssued: Boolean, printerDeviceCode: String?): LabelConfirmStep = when {
    qty == null || qty < 0 -> LabelConfirmStep.Refuse("QTY-001", "Please enter a valid quantity (0 or more).")
    qty == 0.0 || labelIssued -> LabelConfirmStep.Go(print = false)
    printerDeviceCode.isNullOrBlank() -> LabelConfirmStep.Refuse("PRN-001", "Select a printer in Settings.")
    else -> LabelConfirmStep.Go(print = true)
}
