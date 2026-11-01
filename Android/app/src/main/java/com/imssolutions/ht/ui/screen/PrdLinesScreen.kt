package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.QrCode2
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.data.ProductionLine
import com.imssolutions.ht.data.round4
import com.imssolutions.ht.ui.EmptyBox
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtBottomInset
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.LoadingBox
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.vm.PrdOrderViewModel
import com.imssolutions.ht.ui.vm.fmt

@Composable
fun PrdLinesScreen(
    orderNo: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onScanProcess: () -> Unit,
) {
    val vm: PrdOrderViewModel = viewModel()
    val state by vm.state.collectAsState()

    LaunchedEffect(Unit) { vm.load(orderNo) }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = orderNo, onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            val order = state.order
            when {
                state.loading -> LoadingBox()
                order == null -> EmptyBox(t("hpNoOrders", "No target production orders."))
                else -> Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(10.dp)) {
                    PrdCard(t("hpOrderInfo", "Production order")) {
                        PrdRow(t("thItemCode", "Item Code"), order.outputItemCode)
                        PrdRow(t("thItemName", "Item Name"), order.outputItemName ?: "—")
                        PrdRow(t("hpPlanned", "Planned"), "${fmt(order.plannedQty)} ${order.uomCode}")
                        PrdRow(t("hpStatus", "Status"), prdStatusName(order.statusCode))
                    }
                    state.htLines.groupBy { it.processCode }.forEach { (process, lines) ->
                        PrdCard("${state.processName(process)} ($process)") {
                            lines.sortedBy { it.lineNo }.forEach { PrdLineRow(it) }
                        }
                    }
                }
            }
        }

        if (state.order != null) {
            PrdActionBar {
                PrdActionButton(t("hpScanProc", "Scan Process QR"), PrdBlue, icon = Icons.Default.QrCode2, onClick = onScanProcess)
            }
        }
        HtBottomInset()
    }

    PrdErrorDialog(state.error) { vm.clearError() }
}

@Composable
fun PrdLineRow(line: ProductionLine) {
    val remaining = round4(line.plannedQty - line.consumedQty)
    PrdRow(
        "${line.itemCode} ${line.itemName.orEmpty()}",
        "${fmt(line.consumedQty)} / ${fmt(line.plannedQty)} ${line.uomCode}",
        when {
            line.consumedQty > line.plannedQty -> PrdRed
            remaining <= 0 -> PrdGreen
            else -> androidx.compose.material3.MaterialTheme.colorScheme.onSurface
        },
    )
}
