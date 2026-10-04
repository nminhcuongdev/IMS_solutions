package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Print
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.vm.ShippingDetailViewModel
import com.imssolutions.ht.ui.vm.fmt

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ShippingDetailScreen(
    slipNo: String,
    serialNo: String,
    scannedQty: String = "",
    scannedLot: String = "",
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onDone: () -> Unit,
    onOpenSettings: () -> Unit,
) {
    val vm: ShippingDetailViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    ErrorSound(state.error)
    var reasonExpanded by remember { mutableStateOf(false) }
    val snackbar = remember { SnackbarHostState() }

    LaunchedEffect(slipNo, serialNo, scannedQty, scannedLot) {
        vm.load(slipNo, serialNo, scannedQty, scannedLot)
    }

    OnScan { code ->
        when (val scan = com.imssolutions.ht.scan.QrPayload.parse(code)) {
            is com.imssolutions.ht.scan.QrScan.Ok -> scan.payload.lotNo?.let(vm::setLot)
            is com.imssolutions.ht.scan.QrScan.Invalid -> vm.setLot(code.trim())
        }
    }

    val strings = LocalHtStrings.current
    LaunchedEffect(state.toast) {
        state.toast?.let {
            if (state.done) {
                vm.clearToast()
                playSuccessSound()
                onDone()
            } else {
                snackbar.showSnackbar(strings.render(it))
                vm.clearToast()
            }
        }
    }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htShpDetail", "Shipping Detail"), onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            when {
                state.loading -> LoadingBox()
                state.line == null -> EmptyBox(t("htLineNotFound", "Không tìm thấy dòng này."))
                else -> {
                    val line = state.line!!
                    Column(
                        Modifier
                            .fillMaxSize()
                            .verticalScroll(rememberScrollState())
                            .padding(10.dp),
                    ) {
                        ShpCard(t("htOrderInfo", "Order Information")) {
                            ShpRow(t("thSlipNo", "Slip No."), line.slipNo)
                            ShpRow(t("thCustomer", "Customer"), line.customerName ?: "—")
                            ShpRow(t("thGate", "Gate"), line.gateCode ?: "—")
                            ShpRow(t("thShipDate", "Ship Date"), line.scheduledDate ?: "—")
                        }

                        Spacer(Modifier.height(10.dp))

                        ShpCard(t("htItemInfo", "Item Information")) {
                            ShpRow(t("thItemCode", "Item Code"), line.itemCode)
                            ShpRow(t("thItemName", "Item Name"), line.itemName ?: "—")
                            ShpRow(t("thPlannedQty", "Planned"), "${fmt(line.plannedQty)} ${line.unit.orEmpty()}".trim())

                            Spacer(Modifier.height(10.dp))
                            ShpField(t("thLot", "Lot No.")) {
                                OutlinedTextField(
                                    value = state.lotNo,
                                    onValueChange = vm::setLot,
                                    singleLine = true,
                                    shape = RoundedCornerShape(8.dp),
                                    placeholder = { Text(t("htScanOrTypeLot", "Quét hoặc nhập số lô"), fontSize = 13.sp) },
                                    modifier = Modifier.fillMaxWidth(),
                                )
                            }

                            ShpField(t("thActualQty", "Actual")) {
                                OutlinedTextField(
                                    value = state.actualQty,
                                    onValueChange = vm::setActual,
                                    singleLine = true,
                                    isError = state.overShipped,
                                    shape = RoundedCornerShape(8.dp),
                                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                                    suffix = { Text(line.unit.orEmpty(), fontSize = 12.sp) },
                                    modifier = Modifier.fillMaxWidth(),
                                )
                            }
                            if (state.overShipped) {
                                Text(
                                    t("htShipOverPlan", "Không xuất được nhiều hơn số kế hoạch."),
                                    fontSize = 11.sp,
                                    color = Color(0xFFDC3545),
                                )
                            }

                            ShpDiffRow(state.diff)

                            if (state.needsReason) {
                                Spacer(Modifier.height(10.dp))
                                ShpField(t("thShortageReason", "Shortage Reason")) {
                                    ExposedDropdownMenuBox(
                                        expanded = reasonExpanded,
                                        onExpandedChange = { reasonExpanded = it },
                                    ) {
                                        val selected = state.reasons
                                            .firstOrNull { it.reasonCode == state.reasonCode }
                                        OutlinedTextField(
                                            value = selected?.let { "${it.reasonCode} - ${it.reasonName}" }
                                                ?: "",
                                            onValueChange = {},
                                            readOnly = true,
                                            singleLine = true,
                                            placeholder = { Text(t("htSelectReason", "Chọn lý do"), fontSize = 13.sp) },
                                            trailingIcon = {
                                                ExposedDropdownMenuDefaults.TrailingIcon(reasonExpanded)
                                            },
                                            shape = RoundedCornerShape(8.dp),
                                            modifier = Modifier
                                                .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                                                .fillMaxWidth(),
                                        )
                                        ExposedDropdownMenu(
                                            reasonExpanded,
                                            onDismissRequest = { reasonExpanded = false },
                                        ) {
                                            state.reasons.forEach { r ->
                                                DropdownMenuItem(
                                                    text = { Text("${r.reasonCode} - ${r.reasonName}") },
                                                    onClick = {
                                                        reasonExpanded = false
                                                        vm.setReason(r.reasonCode)
                                                    },
                                                )
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (state.line != null) {
            Row(
                Modifier
                    .fillMaxWidth()
                    .background(MaterialTheme.colorScheme.surface)
                    .padding(10.dp),
                horizontalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                Button(
                    onClick = vm::confirm,
                    enabled = !state.busy,
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF28A745)),
                    modifier = Modifier.weight(1f).height(44.dp),
                ) {
                    Icon(Icons.Default.Check, null, tint = Color.White)
                    Spacer(Modifier.width(6.dp))
                    Text(t("stConfirm", "Confirm"), fontWeight = FontWeight.Bold, color = Color.White)
                }

                if (state.showLabel) {
                    Button(
                        onClick = {
                            val deviceCode = ApiClient.tokens.htSettingsBlocking().printerDeviceCode
                            if (deviceCode.isBlank()) onOpenSettings() else vm.issueLabel(deviceCode)
                        },
                        enabled = !state.busy && !state.labelIssued,
                        shape = RoundedCornerShape(8.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF007AC9)),
                        modifier = Modifier.weight(1f).height(44.dp),
                    ) {
                        Icon(Icons.Default.Print, null, tint = Color.White)
                        Spacer(Modifier.width(6.dp))
                        Text(
                            if (state.labelIssued) t("badgeIssued", "Issued") else "Label",
                            fontWeight = FontWeight.Bold,
                            color = Color.White,
                        )
                    }
                }
            }
        }

        SnackbarHost(snackbar)
        HtStatusBar(clock = nowClock(), right = slipNo)
    }

    state.error?.let { (code, message) ->
        AlertDialog(
            onDismissRequest = { vm.clearError() },
            title = { Text(code) },
            text = { Text(t(code, message, message)) },
            confirmButton = { Button(onClick = { vm.clearError() }) { Text(t("thYes", "OK")) } },
        )
    }
}

@Composable
private fun ShpCard(title: String, content: @Composable ColumnScope.() -> Unit) {
    Column(
        Modifier
            .fillMaxWidth()
            .background(MaterialTheme.colorScheme.surface, RoundedCornerShape(10.dp))
            .padding(12.dp),
    ) {
        Text(title, fontSize = 11.sp, fontWeight = FontWeight.Bold, color = Color(0xFF007AC9))
        Spacer(Modifier.height(8.dp))
        content()
    }
}

@Composable
private fun ShpRow(label: String, value: String) {
    Row(
        Modifier.fillMaxWidth().padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(label, fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(
            value,
            fontSize = 12.5.sp,
            fontWeight = FontWeight.SemiBold,
            color = MaterialTheme.colorScheme.onSurface,
        )
    }
}

@Composable
private fun ShpDiffRow(diff: Double?) {
    val color = when {
        diff == null -> MaterialTheme.colorScheme.onSurfaceVariant
        diff == 0.0 -> Color(0xFF28A745)
        else -> Color(0xFFDC3545)
    }
    Row(
        Modifier.fillMaxWidth().padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(t("thDiff", "Diff"), fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(
            when {
                diff == null -> "—"
                diff > 0 -> "+${fmt(diff)}"
                else -> fmt(diff)
            },
            fontSize = 15.sp,
            fontWeight = FontWeight.Bold,
            color = color,
        )
    }
}

@Composable
private fun ShpField(label: String, content: @Composable () -> Unit) {
    Column(Modifier.fillMaxWidth().padding(bottom = 10.dp)) {
        Text(
            label,
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.padding(bottom = 4.dp),
        )
        content()
    }
}
