package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Print
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.ui.BusyOverlay
import com.imssolutions.ht.ui.EmptyBox
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtBottomInset
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.LoadingBox
import com.imssolutions.ht.ui.playSuccessSound
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.vm.PrdLabelViewModel
import com.imssolutions.ht.ui.vm.fmt
import com.imssolutions.ht.ui.vm.fmtDateTime

@Composable
fun PrdLabelScreen(
    orderNo: String,
    requestId: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
) {
    val vm: PrdLabelViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)

    LaunchedEffect(Unit) { vm.load(orderNo, requestId) }
    LaunchedEffect(state.printed) { if (state.printed) playSuccessSound() }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("hpLabelTitle", "Finished Goods Label"), onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            val order = state.order
            when {
                state.loading -> LoadingBox()
                order == null || state.outputs.isEmpty() -> EmptyBox("—")
                else -> Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(10.dp)) {
                    val unit = order.uomCode
                    val plan = state.plan
                    PrdCard(t("hpOutResult", "Production result")) {
                        PrdRow(t("hpOrderNo", "Order No"), order.productionOrderNo)
                        PrdRow(t("thItemCode", "Item Code"), order.outputItemCode)
                        PrdRow(t("thItemName", "Item Name"), order.outputItemName.orEmpty())
                        PrdRow(t("hpLot", "Lot"), state.lot)
                        PrdRow(t("hpActualQty", "Actual produced quantity"), "${fmt(state.qty)} $unit")
                    }
                    PrdCard(t("hpLabelCount", "Label count")) {
                        PrdRow(t("hpStdQty", "Standard qty"), "${fmt(plan.std)} $unit")
                        PrdRow(
                            t("hpFormula", "Actual ÷ standard"),
                            if (plan.std > 1) {
                                "${fmt(state.qty)} ÷ ${fmt(plan.std)} = ${plan.full}" +
                                    if (plan.remainder > 0) " + ${fmt(plan.remainder)}" else ""
                            } else {
                                "${fmt(state.qty)} = ${t("hpLabelWholeShort", "1 label (whole quantity)")}"
                            },
                        )
                        PrdRow(t("hpLabelCount", "Label count"), plan.count.toString())
                        if (plan.remainder > 0 && plan.std > 1) {
                            PrdInfo("${t("hpRemainderRule", "Remainder: the last label carries the remainder quantity")}: ${fmt(plan.remainder)} $unit")
                        }
                    }
                    state.issued?.let {
                        PrdOkNote("${t("hpLabelIssued", "Labels already issued")}: ${fmtDateTime(it.createdAt)}")
                    }
                }
            }
        }

        if (state.order != null && state.outputs.isNotEmpty()) {
            PrdActionBar {
                PrdActionButton(
                    t("labelPrint", "Label"),
                    PrdBlue,
                    enabled = !state.busy && state.issued == null,
                    icon = Icons.Default.Print,
                ) {
                    vm.issue(ApiClient.tokens.htSettingsBlocking().printerDeviceCode.orEmpty())
                }
            }
        }
        HtBottomInset()
    }

    PrdErrorDialog(state.error) { vm.clearError() }
}
