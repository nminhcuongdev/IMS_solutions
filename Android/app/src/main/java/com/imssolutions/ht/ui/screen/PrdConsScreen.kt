package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.data.activeProductionWorks
import com.imssolutions.ht.data.round4
import com.imssolutions.ht.ui.BusyOverlay
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtBottomInset
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.LoadingBox
import com.imssolutions.ht.ui.playSuccessSound
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.vm.PrdConsViewModel
import com.imssolutions.ht.ui.vm.PrdMaterial
import com.imssolutions.ht.ui.vm.fmt
import com.imssolutions.ht.ui.vm.fmtDateTime

@Composable
fun PrdConsScreen(
    orderNo: String,
    processCode: String,
    material: PrdMaterial,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
) {
    val vm: PrdConsViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)

    LaunchedEffect(Unit) { vm.load(orderNo, processCode, material) }
    LaunchedEffect(state.done) { if (state.done != null) playSuccessSound() }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("hpConsTitle", "Input Quantity"), onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            if (state.loading) LoadingBox()
            else {
                val unit = state.item?.uomCode.orEmpty()
                val lines = state.processLines(processCode, material.itemCode)
                val planned = round4(lines.sumOf { it.plannedQty })
                val consumed = round4(lines.sumOf { it.consumedQty })
                val lineNos = lines.map { it.lineNo }.toSet()
                val prev = activeProductionWorks(state.works.consumptions).filter {
                    it.itemCode == material.itemCode &&
                        it.lotNo.orEmpty() == material.lotNo &&
                        (it.lineNo == null || it.lineNo in lineNos)
                }
                val available = vm.preview()?.available ?: 0.0
                val processName = state.lines.firstOrNull { it.processCode == processCode }?.processName ?: processCode

                Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(10.dp)) {
                    PrdCard(t("hpMaterial", "Material")) {
                        PrdRow(t("hpOrderNo", "Order No"), orderNo)
                        PrdRow(t("hpProcess", "Process"), processName)
                        PrdRow(t("thItemCode", "Item Code"), material.itemCode)
                        PrdRow(t("thItemName", "Item Name"), state.item?.itemName.orEmpty())
                        PrdRow(t("hpLot", "Lot"), material.lotNo.ifEmpty { "—" })
                        PrdRow(t("hpAvailable", "Available stock"), "${fmt(available)} $unit")
                    }
                    PrdCard(t("hpPlanVsActual", "Planned / consumed")) {
                        PrdRow(t("hpPlanned", "Planned"), "${fmt(planned)} $unit")
                        PrdRow(t("hpConsumed", "Consumed"), "${fmt(consumed)} $unit")
                        PrdRow(t("hpRemaining", "Remaining"), "${fmt(maxOf(0.0, round4(planned - consumed)))} $unit")
                    }
                    if (prev.isNotEmpty()) {
                        PrdCard(t("hpPrevResults", "Previous input of this QR (same lot)"), accent = PrdOrange) {
                            prev.forEach { PrdRow(fmtDateTime(it.createdAt), "${fmt(it.qty ?: 0.0)} $unit") }
                            PrdRow(t("hpCumulative", "Cumulative"), "${fmt(round4(prev.sumOf { it.qty ?: 0.0 }))} $unit")
                            PrdInfo(t("hpAddNote", "A new input is added to the previous results (not overwritten)."))
                        }
                    }
                    PrdCard(null) {
                        PrdField("${t("hpInputQty", "Input quantity")} ($unit)") {
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
                        }
                    }
                }
            }
        }

        PrdActionBar {
            PrdActionButton(t("stConfirm", "Confirm"), PrdGreen, enabled = !state.busy && !state.loading) { vm.confirm() }
        }
        HtBottomInset()
    }

    PrdErrorDialog(state.error) { vm.clearError() }
    if (state.askNearExpiry) {
        PrdInfoDialog(
            t("INV-002", "This lot is near its expiry date. Continue?"),
            onOk = vm::acceptNearExpiry,
            onCancel = vm::dismissNearExpiry,
        )
    }
    state.done?.let { done ->
        PrdInfoDialog((listOf(t("htConfirmedMsg", "Confirmed.")) + done.extra).joinToString("\n"), onOk = onBack)
    }
}
