package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.ui.BusyOverlay
import com.imssolutions.ht.ui.EmptyBox
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtBottomInset
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.LoadingBox
import com.imssolutions.ht.ui.playSuccessSound
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.vm.PrdOutputViewModel
import com.imssolutions.ht.ui.vm.fmt

private val ReadOnlyBg = Color(0xFFF0F0F0)

@Composable
fun PrdOutputScreen(
    orderNo: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onDone: (String) -> Unit,
) {
    val vm: PrdOutputViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)

    LaunchedEffect(Unit) { vm.load(orderNo) }
    LaunchedEffect(state.doneRequestId) { if (state.doneRequestId != null) playSuccessSound() }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = orderNo, onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            val order = state.order
            when {
                state.loading -> LoadingBox()
                order == null -> EmptyBox(t("hpNoOrders", "No target production orders."))
                else -> Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(10.dp)) {
                    val unit = order.uomCode
                    PrdCard(t("hpOrderInfo", "Production order")) {
                        PrdRow(t("thItemCode", "Item Code"), order.outputItemCode)
                        PrdRow(t("thItemName", "Item Name"), order.outputItemName.orEmpty())
                        PrdRow(t("hpProdLot", "Production lot"), order.productionLotNo?.ifEmpty { null } ?: "—")
                        PrdRow(t("hpPlanned", "Planned"), "${fmt(order.plannedQty)} $unit")
                        PrdRow(t("hpProduced", "Produced"), "${fmt(order.outputQty)} $unit")
                        PrdRow(t("hpStdQty", "Standard qty"), "${state.item?.let { fmt(it.packageQty) } ?: "—"} $unit")
                    }
                    PrdCard(null) {
                        PrdField("${t("hpActualQty", "Actual produced quantity")} ($unit)") {
                            Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                                OutlinedTextField(
                                    value = state.qty,
                                    onValueChange = vm::setQty,
                                    singleLine = true,
                                    shape = RoundedCornerShape(8.dp),
                                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                                    modifier = Modifier.weight(1f),
                                )
                                Text(unit, fontWeight = FontWeight.SemiBold)
                            }
                            state.diffToPlan?.let { diff ->
                                PrdInfo(
                                    "${t("hpDiffToPlan", "Difference to plan")}: ${if (diff > 0) "+" else ""}${fmt(diff)} $unit",
                                    warn = diff > 0,
                                )
                            }
                        }
                        PrdField(t("hpFgLot", "Finished goods lot")) {
                            PrdTextInput(state.lot, vm::setLot, readOnly = !state.lotEditable)
                        }
                        PrdField(t("hpExpiry", "Expiry date")) {
                            PrdTextInput(
                                state.expiry, vm::setExpiry,
                                placeholder = state.item?.expirationPeriod?.takeIf { it > 0 }
                                    ?.let { t("hpExpiryAuto", "Blank = today + {0} days (server date)", it.toInt()) } ?: "yyyy-MM-dd",
                            )
                        }
                        PrdField(t("hpLocation", "Storage location (from the production order)")) {
                            PrdTextInput(order.storageLocationCode, {}, readOnly = true)
                        }
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            Checkbox(
                                checked = state.finalize,
                                onCheckedChange = vm::setFinalize,
                                colors = CheckboxDefaults.colors(checkedColor = PrdBlue),
                            )
                            Text(t("hpFinalize", "Finalize this order (Completed)"), fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                        }
                        val qty = state.qtyValue
                        val plan = state.plan
                        if (qty != null && plan != null) {
                            PrdInfo(
                                if (plan.std <= 1) "${t("hpLabelWhole", "Labels: whole quantity = 1 label")} (${fmt(qty)})"
                                else buildString {
                                    append(t("hpLabelCalc", "Labels")).append(": ").append(fmt(qty)).append(" ÷ ")
                                        .append(fmt(plan.std)).append(" = ").append(plan.count)
                                    if (plan.remainder > 0) {
                                        append(" (").append(plan.full).append(" × ").append(fmt(plan.std))
                                            .append(" + 1 × ").append(fmt(plan.remainder)).append(")")
                                    }
                                },
                            )
                        }
                    }
                }
            }
        }

        if (state.order != null) {
            PrdActionBar {
                PrdActionButton(t("stConfirm", "Confirm"), PrdGreen, enabled = !state.busy) { vm.confirm() }
            }
        }
        HtBottomInset()
    }

    PrdErrorDialog(state.error) { vm.clearError() }
    if (state.askOverPlan) {
        PrdInfoDialog(
            t("hpOverPlan", "The quantity exceeds the planned quantity. Register anyway?"),
            onOk = vm::acceptOverPlan,
            onCancel = vm::dismissOverPlan,
        )
    }
    state.doneRequestId?.let { requestId ->
        PrdInfoDialog(
            (listOf(t("htConfirmedMsg", "Confirmed.")) + state.doneLines.filter { it.isNotBlank() }).joinToString("\n"),
            onOk = { onDone(requestId) },
        )
    }
}

@Composable
fun PrdTextInput(value: String, onChange: (String) -> Unit, readOnly: Boolean = false, placeholder: String? = null) {
    OutlinedTextField(
        value = value,
        onValueChange = onChange,
        readOnly = readOnly,
        singleLine = true,
        shape = RoundedCornerShape(8.dp),
        placeholder = placeholder?.let { { Text(it, fontSize = 13.sp) } },
        colors = if (readOnly) OutlinedTextFieldDefaults.colors(
            unfocusedContainerColor = ReadOnlyBg,
            focusedContainerColor = ReadOnlyBg,
        ) else OutlinedTextFieldDefaults.colors(),
        modifier = Modifier.fillMaxWidth(),
    )
}
