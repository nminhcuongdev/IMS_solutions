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
import com.imssolutions.ht.ui.vm.ReceivingDetailViewModel
import com.imssolutions.ht.ui.vm.fmt

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReceivingDetailScreen(
    poNo: String,
    serialNo: String,
    scannedQty: String = "",
    scannedLot: String = "",
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onDone: () -> Unit,
    onOpenSettings: () -> Unit,
) {
    val vm: ReceivingDetailViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    ErrorSound(state.error)
    var reasonExpanded by remember { mutableStateOf(false) }
    val snackbar = remember { SnackbarHostState() }

    LaunchedEffect(poNo, serialNo) { vm.load(poNo, serialNo, scannedQty, scannedLot) }

    OnScan { code -> vm.onScan(code) }

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
        HtHeader(title = t("htRcvDetail", "Receiving Detail"), onBack = onBack, lang = lang, onLang = onLang)

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
                        DCard(t("htOrderInfo", "Order Information")) {
                            DRow(t("thPoNo", "PO No."), line.poNo)
                            DRow(t("thSupplier", "Supplier"), line.supplierName ?: "—")
                            DRow(t("thRecvDate", "Recv Date"), line.scheduledDate ?: "—")
                        }

                        Spacer(Modifier.height(10.dp))

                        DCard(t("htItemInfo", "Item Information")) {
                            DRow(t("thItemCode", "Item Code"), line.itemCode)
                            DRow(t("thItemName", "Item Name"), line.itemName ?: "—")
                            DRow(
                                t("htPlannedWholeSlip", "Kế hoạch (cả phiếu)"),
                                "${fmt(line.plannedQty)} ${line.unit.orEmpty()}".trim(),
                            )
                            DRow(
                                t("htExpectedPerPackage", "Dự kiến (1 kiện)"),
                                "${fmt(state.expected)} ${line.unit.orEmpty()}".trim() +
                                    (state.packageCount?.let { " · " + t("htPackagesN", "{0} kiện", fmt(it)) } ?: ""),
                            )

                            Spacer(Modifier.height(10.dp))
                            TfField(t("thLot", "Lot No.")) {
                                OutlinedTextField(
                                    value = state.lotNo,
                                    onValueChange = vm::setLot,
                                    singleLine = true,
                                    shape = RoundedCornerShape(8.dp),
                                    placeholder = { Text(t("htScanOrTypeLot", "Quét hoặc nhập số lô"), fontSize = 13.sp) },
                                    modifier = Modifier.fillMaxWidth(),
                                )
                            }

                            TfField(t("thActualQty", "Actual")) {
                                OutlinedTextField(
                                    value = state.actualQty,
                                    onValueChange = vm::setActual,
                                    readOnly = !state.canEditActual,
                                    enabled = state.canEditActual,
                                    singleLine = true,
                                    shape = RoundedCornerShape(8.dp),
                                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                                    suffix = { Text(line.unit.orEmpty(), fontSize = 12.sp) },
                                    colors = OutlinedTextFieldDefaults.colors(
                                        disabledTextColor = MaterialTheme.colorScheme.onSurface,
                                        disabledBorderColor = MaterialTheme.colorScheme.outline,
                                        disabledSuffixColor = MaterialTheme.colorScheme.onSurfaceVariant,
                                    ),
                                    modifier = Modifier.fillMaxWidth(),
                                )
                                if (state.fromScan) {
                                    Text(
                                        t("htFromQrReadOnly", "Lấy từ mã QR trên nhãn — không sửa được"),
                                        fontSize = 11.sp,
                                        color = Color(0xFF007AC9),
                                        modifier = Modifier.padding(top = 4.dp),
                                    )
                                }
                            }

                            DiffRow(state.diff)

                            if (state.needsReason) {
                                Spacer(Modifier.height(10.dp))
                                TfField(t("thShortageReason", "Shortage Reason")) {
                                    ExposedDropdownMenuBox(
                                        expanded = reasonExpanded,
                                        onExpandedChange = { reasonExpanded = it },
                                    ) {
                                        OutlinedTextField(
                                            value = state.reasons
                                                .firstOrNull { it.reasonCode == state.reasonCode }
                                                ?.let { "${it.reasonCode} - ${it.reasonName}" }
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

                            if (state.locationCode.isBlank()) {
                                Row(
                                    Modifier.fillMaxWidth().padding(vertical = 4.dp),
                                    horizontalArrangement = Arrangement.SpaceBetween,
                                    verticalAlignment = Alignment.CenterVertically,
                                ) {
                                    Text(t("thLocation", "Location"), fontSize = 12.sp,
                                        color = MaterialTheme.colorScheme.onSurfaceVariant)
                                    Text(t("htScanLocationLabel", "Quét tem vị trí"), fontSize = 12.5.sp,
                                        fontWeight = FontWeight.SemiBold, color = Color(0xFFE67E00))
                                }
                            } else {
                                DRow(t("thLocation", "Location"), state.locationCode)
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
                if (state.showConfirm) {
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
                }

                if (state.showLabel) {
                    Button(
                        onClick = {
                            val deviceCode = ApiClient.tokens.htSettingsBlocking().printerDeviceCode
                            if (deviceCode.isBlank()) onOpenSettings() else vm.issueLabelAndConfirm(deviceCode)
                        },
                        enabled = !state.busy,
                        shape = RoundedCornerShape(8.dp),
                        colors = ButtonDefaults.buttonColors(containerColor = BrandBlueBtn),
                        modifier = Modifier.weight(1f).height(44.dp),
                    ) {
                        Icon(Icons.Default.Print, null, tint = Color.White)
                        Spacer(Modifier.width(6.dp))
                        Text(t("htLabelConfirm", "Label + Confirm"), fontWeight = FontWeight.Bold, color = Color.White)
                    }
                }
            }
        }

        SnackbarHost(snackbar)
        HtStatusBar(clock = nowClock(), right = poNo)
    }

    state.error?.let { (code, message) ->
        AlertDialog(
            onDismissRequest = { vm.clearError() },
            title = { Text(code) },
            text = {
                Text(if (code == "LOC-001") t("htLoc001Scan", message) else t(code, message, message))
            },
            confirmButton = { Button(onClick = { vm.clearError() }) { Text(t("thYes", "OK")) } },
        )
    }
}

private val BrandBlueBtn = Color(0xFF007AC9)

@Composable
private fun DCard(title: String, content: @Composable ColumnScope.() -> Unit) {
    Column(
        Modifier
            .fillMaxWidth()
            .background(MaterialTheme.colorScheme.surface, RoundedCornerShape(10.dp))
            .padding(12.dp),
    ) {
        Text(
            title,
            fontSize = 11.sp,
            fontWeight = FontWeight.Bold,
            color = BrandBlueBtn,
        )
        Spacer(Modifier.height(8.dp))
        content()
    }
}

@Composable
private fun DRow(label: String, value: String) {
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
private fun DiffRow(diff: Double?) {
    val color = when {
        diff == null -> MaterialTheme.colorScheme.onSurfaceVariant
        diff == 0.0 -> Color(0xFF28A745)
        diff < 0 -> Color(0xFFDC3545)
        else -> Color(0xFF0D6EFD)
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
private fun TfField(label: String, content: @Composable () -> Unit) {
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
