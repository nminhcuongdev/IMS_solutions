package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.BusyOverlay
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtBottomInset
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.LoadingBox
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.vm.PrdMaterial
import com.imssolutions.ht.ui.vm.PrdOrderViewModel
import com.imssolutions.ht.ui.vm.PrdScanMatViewModel

@Composable
fun PrdScanMaterialScreen(
    orderNo: String,
    processCode: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onBackToLines: () -> Unit,
    onAccepted: (PrdMaterial) -> Unit,
) {
    val order: PrdOrderViewModel = viewModel()
    val orderState by order.state.collectAsState()
    val vm: PrdScanMatViewModel = viewModel()
    val state by vm.state.collectAsState()
    var qr by remember { mutableStateOf("") }
    BusyOverlay(state.busy)

    LaunchedEffect(Unit) { order.load(orderNo) }
    LaunchedEffect(state.accepted) {
        state.accepted?.let { vm.consumed(); qr = ""; onAccepted(it) }
    }

    fun submit(raw: String) = vm.submit(raw, orderState.lines, processCode)
    OnScan { raw -> qr = raw; submit(raw) }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("hpScanMat", "Scan Material Label"), onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            if (orderState.loading) LoadingBox()
            else Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(10.dp)) {
                PrdCard("${orderState.processName(processCode)} ($processCode)") {
                    orderState.htLines.filter { it.processCode == processCode }.sortedBy { it.lineNo }.forEach { PrdLineRow(it) }
                }
                PrdCard(t("hpMatLabel", "Material label (product label)")) {
                    PrdField(t("hpQrText", "QR text")) {
                        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                            OutlinedTextField(
                                value = qr,
                                onValueChange = { qr = it },
                                singleLine = true,
                                shape = RoundedCornerShape(8.dp),
                                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Done),
                                keyboardActions = KeyboardActions(onDone = { submit(qr) }),
                                modifier = Modifier.weight(1f),
                            )
                            Button(onClick = { submit(qr) }, shape = RoundedCornerShape(8.dp), modifier = Modifier.height(52.dp)) {
                                Text("OK")
                            }
                        }
                    }
                }
            }
        }

        PrdActionBar {
            PrdActionButton(t("hpBackToLines", "Back to lines"), PrdOrange, icon = null, onClick = onBackToLines)
        }
        HtBottomInset()
    }

    PrdErrorDialog(state.error ?: orderState.error) { vm.clearError(); order.clearError() }
}
