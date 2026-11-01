package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.data.ReturnPlan
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.scan.QrPayload
import com.imssolutions.ht.scan.QrScan
import com.imssolutions.ht.scan.ScanError
import com.imssolutions.ht.scan.ScanLine
import com.imssolutions.ht.scan.ScanMatch
import com.imssolutions.ht.scan.matchScannedItem
import com.imssolutions.ht.ui.distinctKeys
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.statusColor
import com.imssolutions.ht.ui.vm.ItemFilter
import com.imssolutions.ht.ui.vm.ReturnItemsViewModel
import com.imssolutions.ht.ui.vm.fmt

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReturnItemsScreen(
    returnNo: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onOpen: (returnNo: String, serialNo: String, scannedQty: String?, scannedLot: String?) -> Unit,
) {
    val vm: ReturnItemsViewModel = viewModel()
    val state by vm.state.collectAsState()
    var filterExpanded by remember { mutableStateOf(false) }
    var scanError by remember { mutableStateOf<Pair<ScanError, String>?>(null) }
    ErrorSound(scanError)

    LaunchedEffect(returnNo) { vm.load(returnNo) }

    OnScan { code ->
        when (val scan = QrPayload.parse(code)) {
            is QrScan.Invalid -> scanError = scan.error to scan.raw
            is QrScan.Ok -> {
                val lines = state.lines
                val match = matchScannedItem(scan.payload, lines.map { ScanLine(it.itemCode, it.lotNo) })
                when (match) {
                    is ScanMatch.Line -> lines[match.index].let {
                        onOpen(it.returnNo, it.serialNo, scan.payload.variableCount, scan.payload.lotNo)
                    }
                    is ScanMatch.Failed -> scanError = match.error to scan.payload.raw
                }
            }
        }
    }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = returnNo, onBack = onBack, lang = lang, onLang = onLang)

        Column(Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface)) {
            Box(Modifier.padding(start = 10.dp, end = 10.dp, top = 8.dp, bottom = 8.dp)) {
                ExposedDropdownMenuBox(
                    expanded = filterExpanded,
                    onExpandedChange = { filterExpanded = it },
                ) {
                    OutlinedTextField(
                        value = t(state.filter.labelKey, state.filter.label),
                        onValueChange = {},
                        readOnly = true,
                        singleLine = true,
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(filterExpanded) },
                        shape = RoundedCornerShape(8.dp),
                        modifier = Modifier
                            .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                            .fillMaxWidth(),
                    )
                    ExposedDropdownMenu(filterExpanded, onDismissRequest = { filterExpanded = false }) {
                        ItemFilter.entries.forEach { f ->
                            DropdownMenuItem(
                                text = { Text(t(f.labelKey, f.label)) },
                                onClick = { filterExpanded = false; vm.setFilter(f) },
                            )
                        }
                    }
                }
            }
            HorizontalDivider(color = Color(0xFFE2E8F0))
        }

        state.error?.let { ErrorBar(it.first, it.second) }

        Box(Modifier.weight(1f)) {
            when {
                state.loading -> LoadingBox()
                state.visible.isEmpty() && state.error == null -> EmptyBox(t("htNoResults", "Không có mặt hàng nào khớp bộ lọc."))
                else -> LazyColumn(contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp)) {
                    val keys = distinctKeys(state.visible.map { it.lineKey })
                    itemsIndexed(state.visible, key = { i, _ -> keys[i] }) { _, line ->
                        ReturnLineCard(line) { onOpen(line.returnNo, line.serialNo, null, null) }
                    }
                }
            }
        }

        HtBottomInset()
    }

    scanError?.let { (err, raw) ->
        AlertDialog(
            onDismissRequest = { scanError = null },
            title = { Text(err.code) },
            text = {
                Column {
                    Text(err.message)
                    Spacer(Modifier.height(8.dp))
                    Text(
                        raw.take(120),
                        fontSize = 11.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
            },
            confirmButton = { Button(onClick = { scanError = null }) { Text(t("thYes", "OK")) } },
        )
    }
}

@Composable
private fun ReturnLineCard(line: ReturnPlan, onClick: () -> Unit) {
    Row(
        Modifier
            .fillMaxWidth()
            .padding(bottom = 7.dp)
            .clip(RoundedCornerShape(10.dp))
            .background(MaterialTheme.colorScheme.surface)
            .clickable(onClick = onClick)
            .height(IntrinsicSize.Min),
    ) {
        Box(Modifier.width(4.dp).fillMaxHeight().background(statusColor(line.status)))
        Column(Modifier.weight(1f).padding(horizontal = 12.dp, vertical = 11.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(
                    line.itemCode,
                    fontSize = 12.5.sp,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onSurface,
                )
                Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                    if (line.labelIssued) IssuedBadge()
                    HiBadge(line.status)
                }
            }
            Spacer(Modifier.height(3.dp))
            Text(
                line.itemName ?: "—",
                fontSize = 12.sp,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1,
            )
            if (!line.lotNo.isNullOrBlank()) {
                Spacer(Modifier.height(2.dp))
                Text(
                    "Lot ${line.lotNo}",
                    fontSize = 11.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
            Spacer(Modifier.height(6.dp))
            Row(horizontalArrangement = Arrangement.spacedBy(16.dp)) {
                RtnQtyCell(t("thPlannedQty", "Dự kiến"), fmt(line.expectedQty), line.unit)
                RtnQtyCell(t("thActualQty", "Đã nhận lại"), fmt(line.actualQty), line.unit)
                Spacer(Modifier.weight(1f))
                Text(
                    "Dòng ${line.serialNo}",
                    fontSize = 11.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                    modifier = Modifier.align(Alignment.Bottom),
                )
            }
        }
    }
}

@Composable
private fun RtnQtyCell(label: String, value: String, unit: String?) {
    Column {
        Text(label, fontSize = 10.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Row(verticalAlignment = Alignment.Bottom) {
            Text(value, fontSize = 15.sp, fontWeight = FontWeight.Bold)
            if (!unit.isNullOrBlank()) {
                Spacer(Modifier.width(2.dp))
                Text(unit, fontSize = 10.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
    }
}
