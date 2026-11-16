package com.imssolutions.ht.ui.screen

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BadgeNg
import com.imssolutions.ht.ui.theme.BadgeOk
import com.imssolutions.ht.ui.theme.BrandBlue
import com.imssolutions.ht.ui.vm.StocktakingViewModel
import com.imssolutions.ht.ui.vm.fmt

@Composable
fun StocktakingDetailScreen(
    vm: StocktakingViewModel,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
) {
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    val g = state.selected
    val snackbar = remember { SnackbarHostState() }
    val strings = LocalHtStrings.current

    OnScan { raw -> vm.onScan(raw) }

    LaunchedEffect(state.toast) {
        state.toast?.let { snackbar.showSnackbar(strings.render(it)); vm.clearToast() }
    }
    LaunchedEffect(state.saved) { if (state.saved) { playSuccessSound(); vm.closeDetail(); onBack() } }

    val leave = { vm.closeDetail(); onBack() }
    BackHandler(onBack = leave)

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htStLabel", "Stocktaking"), onBack = leave, lang = lang, onLang = onLang)

        state.error?.let { ErrorBar(it.first, it.second) }

        Column(
            Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(10.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            if (g == null) {
                EmptyBox(t("noResults", "No rows were found."))
            } else {
                StCard(t("htItemInfo", "Item Information")) {
                    StRow(t("thItemCode", "Item Code"), g.itemCode)
                    StRow(t("thItemName", "Item Name"), g.itemName ?: "—")
                    StRow(t("thLot", "Lot"), g.lotNo?.takeIf { it.isNotBlank() } ?: "—")
                    StRow(t("thLocation", "Location"), g.locationText)
                    StRow(
                        t("thPlannedQty", "Planned"),
                        "${fmt(g.plannedQty)} ${g.unit.orEmpty()}".trim(),
                    )
                    StRow(t("htLabelCount", "Label Count"), g.plannedLabels.toString())
                    StProgressRow(counted = state.progress.first, total = state.progress.second)

                    Spacer(Modifier.height(4.dp))
                    StField(t("thActualQty", "Actual")) {
                        Row(verticalAlignment = Alignment.CenterVertically) {
                            OutlinedTextField(
                                value = state.counted,
                                onValueChange = vm::setCounted,
                                modifier = Modifier.weight(1f),
                                singleLine = true,
                                keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                                shape = RoundedCornerShape(8.dp),
                                placeholder = { Text("0", fontSize = 13.sp) },
                            )
                            Spacer(Modifier.width(8.dp))
                            Text(
                                t("htLabelCount", "Label Count"),
                                fontSize = 12.sp,
                                color = MaterialTheme.colorScheme.onSurfaceVariant,
                            )
                        }
                    }

                    Text(
                        t("htStZeroNote", "If no package is found, enter 0; that is a result too."),
                        fontSize = 11.5.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            }
        }

        Row(Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface).padding(10.dp)) {
            Button(
                onClick = vm::save,
                enabled = g != null && !state.busy,
                modifier = Modifier.fillMaxWidth().height(46.dp),
                shape = RoundedCornerShape(8.dp),
            ) {
                if (state.busy) {
                    CircularProgressIndicator(
                        Modifier.size(18.dp), strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary,
                    )
                } else {
                    Text(t("btnSave", "Save"), fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
                }
            }
        }

        SnackbarHost(snackbar)
        HtBottomInset()
    }
}

@Composable
private fun StCard(title: String, content: @Composable ColumnScope.() -> Unit) {
    Column(
        Modifier.fillMaxWidth()
            .background(MaterialTheme.colorScheme.surface, RoundedCornerShape(10.dp))
            .padding(12.dp),
    ) {
        Text(title, fontSize = 11.sp, fontWeight = FontWeight.Bold, color = BrandBlue)
        Spacer(Modifier.height(8.dp))
        content()
    }
}

@Composable
private fun StRow(label: String, value: String) {
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
private fun StProgressRow(counted: Int, total: Int) {
    val color = when {
        counted == total -> BadgeOk
        counted > total -> BrandBlue
        else -> BadgeNg
    }
    Row(
        Modifier.fillMaxWidth().padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(
            t("htScannedProgress", "Scanned"),
            fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
        Text("$counted/$total", fontSize = 15.sp, fontWeight = FontWeight.Bold, color = color)
    }
}

@Composable
private fun StField(label: String, content: @Composable () -> Unit) {
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
