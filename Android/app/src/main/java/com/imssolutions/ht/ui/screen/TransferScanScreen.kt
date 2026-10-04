package com.imssolutions.ht.ui.screen

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.data.TransferLine
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BadgeOk
import com.imssolutions.ht.ui.theme.BrandBlue
import com.imssolutions.ht.ui.vm.TransferViewModel
import com.imssolutions.ht.ui.vm.fmt

@Composable
fun TransferScanScreen(
    vm: TransferViewModel,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onDone: () -> Unit,
) {
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    var confirmDiscard by remember { mutableStateOf(false) }
    val snackbar = remember { SnackbarHostState() }

    OnScan { raw -> vm.onScan(raw) }

    val strings = LocalHtStrings.current
    LaunchedEffect(state.toast) {
        state.toast?.let { snackbar.showSnackbar(strings.render(it)); vm.clearToast() }
    }
    LaunchedEffect(state.done) { if (state.done) { playSuccessSound(); vm.resetScans(); onDone() } }

    val leave = { if (state.lines.isEmpty()) onBack() else { confirmDiscard = true } }
    BackHandler(onBack = leave)

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htTransfer", "Location Transfer"), onBack = leave, lang = lang, onLang = onLang)

        state.error?.let { ErrorBar(it.first, it.second) }

        Row(
            Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface)
                .padding(horizontal = 14.dp, vertical = 10.dp),
            horizontalArrangement = Arrangement.SpaceBetween,
        ) {
            Text(t("htToLoc", "To Location"), fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
            Text(
                state.toLocationCode ?: "—",
                fontSize = 13.sp, fontWeight = FontWeight.Bold, color = BrandBlue,
            )
        }
        HorizontalDivider(color = Color(0xFFE2E8F0))

        Box(Modifier.weight(1f)) {
            if (state.empty) ScanHint(busy = state.busy)
            else LazyColumn(contentPadding = PaddingValues(horizontal = 10.dp, vertical = 8.dp)) {
                items(state.lines, key = { it.key }) { l ->
                    ScannedRow(line = l, onRemove = { vm.remove(l.key) })
                }
            }
        }

        Row(
            Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface).padding(10.dp),
        ) {
            Button(
                onClick = vm::confirm,
                enabled = !state.busy,
                modifier = Modifier.fillMaxWidth().height(46.dp),
                shape = RoundedCornerShape(8.dp),
            ) {
                if (state.busy) {
                    CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp, color = Color.White)
                } else {
                    Text(t("btnSave", "Save"), fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
                }
            }
        }

        SnackbarHost(snackbar)
        HtStatusBar(
            clock = nowClock(),
            right = if (state.empty) t("htRowCount", "{0} dòng", 0) else t("htRowCount", "{0} dòng", state.lines.size) + " · " + fmt(state.totalQty),
        )
    }

    if (confirmDiscard) {
        AlertDialog(
            onDismissRequest = { confirmDiscard = false },
            title = { Text(t("confirmTitle", "Bỏ danh sách đã quét?")) },
            text = { Text(t("htScannedRowsLost", "{0} dòng vừa quét sẽ mất.", state.lines.size)) },
            confirmButton = {
                TextButton(onClick = { confirmDiscard = false; vm.resetScans(); onBack() }) { Text(t("htDiscard", "Bỏ")) }
            },
            dismissButton = {
                TextButton(onClick = { confirmDiscard = false }) { Text(t("htStay", "Ở lại")) }
            },
        )
    }
}

@Composable
private fun ScanHint(busy: Boolean) {
    Box(Modifier.fillMaxSize().padding(24.dp), contentAlignment = Alignment.Center) {
        if (busy) CircularProgressIndicator()
        else Text(
            t("htPleaseScanQr", "Please scan QR"),
            fontSize = 15.sp,
            fontWeight = FontWeight.SemiBold,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}

@Composable
private fun ScannedRow(line: TransferLine, onRemove: () -> Unit) {
    Row(
        Modifier.fillMaxWidth().padding(bottom = 8.dp)
            .background(MaterialTheme.colorScheme.surface, RoundedCornerShape(10.dp))
            .height(IntrinsicSize.Min),
    ) {
        Box(Modifier.width(4.dp).fillMaxHeight().background(BadgeOk))
        Column(Modifier.weight(1f).padding(12.dp)) {
            Row(verticalAlignment = Alignment.CenterVertically) {
                Text(line.itemCode, Modifier.weight(1f), fontSize = 12.5.sp, fontWeight = FontWeight.Bold)
                Text(
                    fmt(line.qty),
                    fontSize = 12.5.sp, fontWeight = FontWeight.Bold, textAlign = TextAlign.End,
                )
                Text(
                    line.unit ?: "",
                    Modifier.padding(start = 6.dp),
                    fontSize = 11.5.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                IconButton(onClick = onRemove, modifier = Modifier.size(28.dp)) {
                    Icon(Icons.Default.Close, contentDescription = t("htRemoveRow", "Bỏ dòng này"), tint = BrandBlue)
                }
            }
            Text(
                buildString {
                    append(line.itemName ?: "—")
                    line.lotNo?.let { append("  (Lot: ").append(it).append(")") }
                },
                fontSize = 11.5.sp,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
            Text(
                t("htFromLocation", "Từ") + ": " + (line.locationCode ?: "—"),
                fontSize = 11.sp,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
            )
        }
    }
}
