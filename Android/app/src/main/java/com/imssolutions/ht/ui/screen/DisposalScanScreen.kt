package com.imssolutions.ht.ui.screen

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
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.data.DisposalLine
import androidx.activity.compose.BackHandler
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BadgeNg
import com.imssolutions.ht.ui.theme.BadgeOk
import com.imssolutions.ht.ui.vm.DisposalViewModel
import com.imssolutions.ht.ui.vm.fmt

@Composable
fun DisposalScanScreen(
    vm: DisposalViewModel,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onNext: () -> Unit,
) {
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    var confirmDiscard by remember { mutableStateOf(false) }

    OnScan { raw -> vm.onScan(raw) }

    val leave = { if (state.lines.isEmpty()) onBack() else { confirmDiscard = true } }
    BackHandler(onBack = leave)

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htDisposal", "Disposal"), onBack = leave, lang = lang, onLang = onLang)

        state.error?.let { ErrorBar(it.first, it.second) }

        Box(Modifier.weight(1f)) {
            when {
                state.empty -> ScanPrompt(busy = state.busy)
                else -> LazyColumn(contentPadding = PaddingValues(horizontal = 10.dp, vertical = 8.dp)) {
                    items(state.lines, key = { it.key }) { l ->
                        ScannedCard(
                            line = l,
                            onQty = { vm.setQty(l.key, it) },
                            onRemove = { vm.remove(l.key) },
                        )
                    }
                }
            }
        }

        Row(
            Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface).padding(10.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Button(
                onClick = { if (state.lines.isEmpty()) vm.confirm() else onNext() },
                enabled = !state.busy && !state.hasBadQty,
                modifier = Modifier.fillMaxWidth().height(46.dp),
                shape = RoundedCornerShape(8.dp),
            ) { Text(t("htNext", "Next"), fontSize = 14.sp, fontWeight = FontWeight.SemiBold) }
        }

        HtBottomInset()
    }

    if (confirmDiscard) {
        AlertDialog(
            onDismissRequest = { confirmDiscard = false },
            title = { Text(t("confirmTitle", "Bỏ danh sách đã quét?")) },
            text = { Text(t("htScannedRowsLost", "{0} dòng vừa quét sẽ mất.", state.lines.size)) },
            confirmButton = {
                TextButton(onClick = { confirmDiscard = false; vm.reset(); onBack() }) { Text(t("htDiscard", "Bỏ")) }
            },
            dismissButton = {
                TextButton(onClick = { confirmDiscard = false }) { Text(t("htStay", "Ở lại")) }
            },
        )
    }
}

@Composable
private fun ScanPrompt(busy: Boolean) {
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
private fun ScannedCard(line: DisposalLine, onQty: (String) -> Unit, onRemove: () -> Unit) {
    val bad = line.qty <= 0 || line.overStock
    Row(
        Modifier.fillMaxWidth().padding(bottom = 8.dp)
            .background(MaterialTheme.colorScheme.surface, RoundedCornerShape(10.dp))
            .height(IntrinsicSize.Min),
    ) {
        Box(Modifier.width(4.dp).fillMaxHeight().background(if (bad) BadgeNg else BadgeOk))
        Column(Modifier.weight(1f).padding(12.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(line.itemCode, fontSize = 12.5.sp, fontWeight = FontWeight.Bold)
                IconButton(onClick = onRemove, modifier = Modifier.size(28.dp)) {
                    Icon(Icons.Default.Close, contentDescription = t("htRemoveRow", "Bỏ dòng này"), tint = BadgeNg)
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
            Spacer(Modifier.height(8.dp))
            Row(verticalAlignment = Alignment.CenterVertically) {
                OutlinedTextField(
                    value = fmt(line.qty),
                    onValueChange = onQty,
                    singleLine = true,
                    isError = bad,
                    label = { Text(t("thQty", "Qty"), fontSize = 11.sp) },
                    suffix = { Text(line.unit ?: "", fontSize = 12.sp) },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Number),
                    shape = RoundedCornerShape(8.dp),
                    modifier = Modifier.width(150.dp),
                )
                Spacer(Modifier.width(10.dp))
                Text(
                    t("htStockOnHand", "Tồn") + ": " + fmt(line.stockQty),
                    fontSize = 11.5.sp,
                    color = if (bad) BadgeNg else MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
    }
}
