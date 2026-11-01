package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.OutlinedTextField
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.KeyboardType
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
import com.imssolutions.ht.ui.vm.PrdReturnViewModel
import com.imssolutions.ht.ui.vm.fmt

@Composable
fun PrdReturnScreen(
    orderNo: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
) {
    val vm: PrdReturnViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)

    LaunchedEffect(Unit) { vm.load(orderNo) }
    LaunchedEffect(state.done) { if (state.done != null) playSuccessSound() }

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
                        PrdRow(t("thItemName", "Item Name"), order.outputItemName.orEmpty())
                        PrdRow(t("hpStatus", "Status"), prdStatusName(order.statusCode))
                    }
                    PrdInfo(t("hpRetHint", "Enter the leftover quantity to return to stock (up to the net input)."))
                    Box(Modifier.padding(top = 8.dp))
                    state.rows.forEachIndexed { i, row ->
                        PrdCard("${row.itemCode} ${row.itemName.orEmpty()}") {
                            PrdRow(t("hpLot", "Lot"), row.lotNo?.ifEmpty { null } ?: "—")
                            PrdRow(t("hpExpiry", "Expiry"), row.expiryDate?.take(10)?.replace('-', '/') ?: "—")
                            PrdRow(t("hpRetNet", "Net input"), "${fmt(row.net)} ${row.unit}")
                            PrdField(t("hpRetQty", "Return qty")) {
                                OutlinedTextField(
                                    value = state.inputs[i].orEmpty(),
                                    onValueChange = { vm.setInput(i, it) },
                                    singleLine = true,
                                    shape = RoundedCornerShape(8.dp),
                                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Decimal),
                                    modifier = Modifier.fillMaxWidth(),
                                )
                            }
                        }
                    }
                    if (state.rows.isEmpty()) EmptyBox(t("hpRetNone", "No returnable material."))
                }
            }
        }

        if (state.order != null) {
            PrdActionBar {
                PrdActionButton(t("stConfirm", "Confirm"), PrdGreen, enabled = !state.busy && state.rows.isNotEmpty()) { vm.confirm() }
            }
        }
        HtBottomInset()
    }

    PrdErrorDialog(state.error) { vm.clearError() }
    if (state.done != null) {
        PrdInfoDialog(t("htConfirmedMsg", "Confirmed."), onOk = onBack)
    }
}
