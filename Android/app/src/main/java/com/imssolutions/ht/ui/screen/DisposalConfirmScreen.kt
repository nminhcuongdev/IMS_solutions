package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.data.DisposalLine
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BadgeNg
import com.imssolutions.ht.ui.theme.BrandBlue
import com.imssolutions.ht.ui.vm.DisposalViewModel
import com.imssolutions.ht.ui.vm.fmt

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun DisposalConfirmScreen(
    vm: DisposalViewModel,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onDone: () -> Unit,
) {
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    var reasonExpanded by remember { mutableStateOf(false) }
    val snackbar = remember { SnackbarHostState() }

    val strings = LocalHtStrings.current
    LaunchedEffect(state.toast) {
        state.toast?.let { snackbar.showSnackbar(strings.render(it)); vm.clearToast() }
    }
    LaunchedEffect(state.done) { if (state.done) { playSuccessSound(); vm.reset(); onDone() } }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htDisposal", "Disposal"), onBack = onBack, lang = lang, onLang = onLang)

        state.error?.let { ErrorBar(it.first, it.second) }

        Column(
            Modifier.weight(1f).verticalScroll(rememberScrollState())
                .padding(horizontal = 10.dp, vertical = 8.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            DCard(t("htDisposalReasonSection", "Disposal Reason")) {
                ExposedDropdownMenuBox(
                    expanded = reasonExpanded,
                    onExpandedChange = { reasonExpanded = it },
                ) {
                    OutlinedTextField(
                        value = state.reasons.firstOrNull { it.reasonCode == state.reasonCode }
                            ?.let { "${it.reasonCode} — ${it.reasonName}" } ?: "— chọn lý do —",
                        onValueChange = {},
                        readOnly = true,
                        singleLine = true,
                        isError = state.reasonCode == null,
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(reasonExpanded) },
                        shape = RoundedCornerShape(8.dp),
                        modifier = Modifier
                            .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                            .fillMaxWidth(),
                    )
                    ExposedDropdownMenu(reasonExpanded, onDismissRequest = { reasonExpanded = false }) {
                        state.reasons.forEach { r ->
                            DropdownMenuItem(
                                text = { Text("${r.reasonCode} — ${r.reasonName}", fontSize = 13.sp) },
                                onClick = { reasonExpanded = false; vm.setReason(r.reasonCode) },
                            )
                        }
                    }
                }
            }

            DCard(t("htItemInfo", "Items")) {
                ConfirmTableHead()
                state.lines.forEach { ConfirmRow(it) }
                HorizontalDivider(color = Color(0xFFE2E8F0))
                Row(
                    Modifier.fillMaxWidth().padding(top = 6.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                ) {
                    Text(t("htTotal", "Total"), fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                    Text(
                        fmt(state.totalQty),
                        fontSize = 12.5.sp, fontWeight = FontWeight.Bold, color = BadgeNg,
                    )
                }
            }
        }

        Row(
            Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface).padding(10.dp),
        ) {
            Button(
                onClick = vm::confirm,
                enabled = !state.busy && !state.empty,
                colors = ButtonDefaults.buttonColors(containerColor = BadgeNg),
                modifier = Modifier.fillMaxWidth().height(46.dp),
                shape = RoundedCornerShape(8.dp),
            ) {
                if (state.busy) CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp, color = Color.White)
                else Text(t("stConfirm", "Confirm"), fontSize = 14.sp, fontWeight = FontWeight.SemiBold)
            }
        }

        SnackbarHost(snackbar)
        HtBottomInset()
    }
}

@Composable
private fun DCard(title: String, content: @Composable ColumnScope.() -> Unit) {
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
private fun ConfirmTableHead() {
    Row(Modifier.fillMaxWidth().padding(bottom = 4.dp)) {
        CHCell(t("thItemCode", "Item Code"), 1.8f)
        CHCell(t("thLot", "Lot"), 1.5f)
        CHCell(t("thQty", "Qty"), 1f, TextAlign.End)
        CHCell(t("thUnit", "Unit"), 0.9f, pad = 6.dp)
    }
    HorizontalDivider(color = Color(0xFFE2E8F0))
}

@Composable
private fun RowScope.CHCell(
    text: String,
    weight: Float,
    align: TextAlign = TextAlign.Start,
    pad: androidx.compose.ui.unit.Dp = 0.dp,
) {
    Text(
        text,
        Modifier.weight(weight).padding(start = pad),
        fontSize = 10.5.sp,
        fontWeight = FontWeight.Bold,
        textAlign = align,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
    )
}

@Composable
private fun ConfirmRow(l: DisposalLine) {
    Column(Modifier.fillMaxWidth().padding(vertical = 5.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(l.itemCode, Modifier.weight(1.8f), fontSize = 11.5.sp, fontWeight = FontWeight.SemiBold)
            Text(l.lotNo ?: "—", Modifier.weight(1.5f), fontSize = 11.5.sp)
            Text(
                fmt(l.qty), Modifier.weight(1f),
                fontSize = 11.5.sp, fontWeight = FontWeight.Bold,
                textAlign = TextAlign.End, color = BadgeNg,
            )
            Text(l.unit ?: "—", Modifier.weight(0.9f).padding(start = 6.dp), fontSize = 11.5.sp)
        }
        Text(
            l.itemName ?: "—",
            fontSize = 11.sp,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}
