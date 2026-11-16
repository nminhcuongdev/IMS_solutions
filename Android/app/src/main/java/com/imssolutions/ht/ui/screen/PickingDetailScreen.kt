package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
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
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.vm.PickingDetailViewModel
import com.imssolutions.ht.ui.vm.fmt

@Composable
fun PickingDetailScreen(
    soNo: String,
    serialNo: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onDone: () -> Unit,
) {
    val vm: PickingDetailViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    ErrorSound(state.error)
    val snackbar = remember { SnackbarHostState() }

    LaunchedEffect(soNo, serialNo) { vm.load(soNo, serialNo) }

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
        HtHeader(title = t("htPickDetail", "Picking Detail"), onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            when {
                state.loading -> LoadingBox()
                state.line == null -> EmptyBox(t("htLineNotFound", "This line was not found."))
                else -> {
                    val line = state.line!!
                    Column(
                        Modifier
                            .fillMaxSize()
                            .verticalScroll(rememberScrollState())
                            .padding(10.dp),
                    ) {
                        PickCard(t("htOrderInfo", "Order Information")) {
                            PickRow(t("thSlipNo", "SO No."), line.soNo)
                            if (state.locationEditable) {
                                PickField(t("thLocation", "Location")) {
                                    OutlinedTextField(
                                        value = state.locationCode,
                                        onValueChange = vm::setLocation,
                                        singleLine = true,
                                        shape = RoundedCornerShape(8.dp),
                                        placeholder = { Text(t("htScanOrTypeLocation", "Scan or type the location code"), fontSize = 13.sp) },
                                        modifier = Modifier.fillMaxWidth(),
                                    )
                                }
                                Text(
                                    t("htPickNoPlanLocation", "The plan has no location; enter where the goods were taken."),
                                    fontSize = 11.sp,
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                )
                            } else {
                                PickRow(
                                    t("thLocation", "Location"),
                                    listOfNotNull(line.locationCode, line.locationName)
                                        .joinToString(" · "),
                                )
                            }
                            PickRow(t("thShipDate", "Pick Date"), line.scheduledDate ?: "—")
                        }

                        Spacer(Modifier.height(10.dp))

                        PickCard(t("htItemInfo", "Item Information")) {
                            PickRow(t("thItemCode", "Item Code"), line.itemCode)
                            PickRow(t("thItemName", "Item Name"), line.itemName ?: "—")
                            PickRow(t("thPlannedQty", "Planned"), "${fmt(line.plannedQty)} ${line.unit.orEmpty()}".trim())
                            if (line.isPartial) {
                                PickRow(t("htScannedProgress", "Picked so far"), "${fmt(line.pickedQty)} ${line.unit.orEmpty()}".trim())
                                Text(
                                    t("htPickTotalHint", "Enter the TOTAL picked, not the extra amount."),
                                    fontSize = 11.sp,
                                    color = Color(0xFFB26A00),
                                )
                            }

                            Spacer(Modifier.height(10.dp))
                            PickField(t("thLot", "Lot")) {
                                OutlinedTextField(
                                    value = state.lotNo,
                                    onValueChange = vm::setLot,
                                    singleLine = true,
                                    readOnly = state.lotFromLabel,
                                    shape = RoundedCornerShape(8.dp),
                                    placeholder = { Text(t("htScanProductLabel", "Scan the product label"), fontSize = 13.sp) },
                                    modifier = Modifier.fillMaxWidth(),
                                )
                            }
                            Spacer(Modifier.height(10.dp))
                            PickField(t("thActualQty", "Actual")) {
                                OutlinedTextField(
                                    value = state.actualQty,
                                    onValueChange = vm::setActual,
                                    singleLine = true,
                                    isError = state.overPicked,
                                    shape = RoundedCornerShape(8.dp),
                                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                                    suffix = { Text(line.unit.orEmpty(), fontSize = 12.sp) },
                                    modifier = Modifier.fillMaxWidth(),
                                )
                            }
                            if (state.overPicked) {
                                Text(
                                    t("htPickOverPlan", "You cannot pick more than planned."),
                                    fontSize = 11.sp,
                                    color = Color(0xFFDC3545),
                                )
                            }
                            if (state.short) {
                                Text(
                                    t("htPickShortHint", "Short pick: the line stays Not Picked so it can be picked later."),
                                    fontSize = 11.sp,
                                    color = Color(0xFFB26A00),
                                )
                            }

                            PickDiffRow(state.diff)
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
            ) {
                Button(
                    onClick = vm::confirm,
                    enabled = !state.busy,
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF28A745)),
                    modifier = Modifier.fillMaxWidth().height(44.dp),
                ) {
                    Icon(Icons.Default.Check, null, tint = Color.White)
                    Spacer(Modifier.width(6.dp))
                    Text(t("stConfirm", "Confirm"), fontWeight = FontWeight.Bold, color = Color.White)
                }
            }
        }

        SnackbarHost(snackbar)
        HtBottomInset()
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
private fun PickCard(title: String, content: @Composable ColumnScope.() -> Unit) {
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
private fun PickRow(label: String, value: String) {
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
private fun PickDiffRow(diff: Double?) {
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
private fun PickField(label: String, content: @Composable () -> Unit) {
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
