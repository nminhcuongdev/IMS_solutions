package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
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
import com.imssolutions.ht.ui.vm.PrdOrderViewModel
import com.imssolutions.ht.ui.vm.fmt
import com.imssolutions.ht.ui.vm.fmtDateTime

@Composable
fun PrdReceiptScreen(
    orderNo: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
) {
    val vm: PrdOrderViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)

    LaunchedEffect(orderNo) { vm.load(orderNo) }
    LaunchedEffect(state.done) { if (state.done != null) playSuccessSound() }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("hpMatReceipt", "Material Receipt"), onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            val order = state.order
            when {
                state.loading -> LoadingBox()
                order == null -> EmptyBox(t("hpNoOrders", "No target production orders."))
                else -> Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(10.dp)) {
                    PrdCard(t("hpOrderInfo", "Production order")) {
                        PrdRow(t("hpOrderNo", "Order No"), order.productionOrderNo)
                        PrdRow(t("thItemCode", "Item Code"), order.outputItemCode)
                        PrdRow(t("thItemName", "Item Name"), order.outputItemName ?: "—")
                        PrdRow(t("hpPlanned", "Planned"), "${fmt(order.plannedQty)} ${order.uomCode}")
                        PrdRow(t("hpHtInput", "HT input"), "${state.htLines.size} / ${state.lines.size}")
                    }
                    PrdCard(t("hpMaterials", "Materials")) {
                        state.lines.sortedBy { it.lineNo }.forEach { l ->
                            PrdRow("${l.processName ?: l.processCode} / ${l.itemCode}", "${fmt(l.plannedQty)} ${l.uomCode}")
                        }
                    }
                    state.receivedAt?.let {
                        PrdOkNote("${t("hpAlreadyReceived", "Already registered")}: ${fmtDateTime(it)}")
                    }
                    if (state.htLines.isEmpty()) {
                        PrdInfo(t("hpNoHtNote", "HT input registration is not required for this order. Receipt can still be recorded."))
                    }
                }
            }
        }

        if (state.order != null) {
            PrdActionBar {
                PrdActionButton(t("hpReceived", "Received"), PrdGreen, enabled = !state.busy) { vm.confirmReceipt() }
            }
        }
        HtBottomInset()
    }

    PrdErrorDialog(state.error) { vm.clearError() }
    if (state.done != null) {
        PrdInfoDialog(t("htConfirmedMsg", "Confirmed."), onOk = onBack)
    }
}
