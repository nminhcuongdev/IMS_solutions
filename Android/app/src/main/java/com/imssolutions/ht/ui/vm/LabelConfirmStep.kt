package com.imssolutions.ht.ui.vm

sealed interface LabelConfirmStep {
    data class Refuse(val code: String, val message: String) : LabelConfirmStep
    data class Go(val print: Boolean) : LabelConfirmStep
}

fun labelConfirmStep(qty: Double?, labelIssued: Boolean, printerDeviceCode: String?): LabelConfirmStep = when {
    qty == null || qty < 0 -> LabelConfirmStep.Refuse("QTY-001", "Vui lòng nhập số lượng hợp lệ (từ 0 trở lên).")
    qty == 0.0 || labelIssued -> LabelConfirmStep.Go(print = false)
    printerDeviceCode.isNullOrBlank() -> LabelConfirmStep.Refuse("PRN-001", "Chưa chọn máy in. Vào Settings để đặt.")
    else -> LabelConfirmStep.Go(print = true)
}
