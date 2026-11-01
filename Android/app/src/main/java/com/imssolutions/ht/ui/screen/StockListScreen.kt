package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
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
import com.imssolutions.ht.data.ExpiryState
import com.imssolutions.ht.data.StockGroup
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BadgeNg
import com.imssolutions.ht.ui.theme.BadgeOk
import com.imssolutions.ht.ui.theme.BadgeWarn
import com.imssolutions.ht.ui.vm.StockFilter
import com.imssolutions.ht.ui.vm.StockListViewModel
import com.imssolutions.ht.ui.vm.fmt

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StockListScreen(
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onOpen: (String) -> Unit,
) {
    val vm: StockListViewModel = viewModel()
    val state by vm.state.collectAsState()
    var filterExpanded by remember { mutableStateOf(false) }

    OnScan { code -> vm.setQuery(code) }

    LaunchedEffect(Unit) { vm.load() }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htInventory", "Stock Inquiry"), onBack = onBack, lang = lang, onLang = onLang)

        Column(Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface)) {
            Box(Modifier.padding(start = 10.dp, end = 10.dp, top = 8.dp)) {
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
                        StockFilter.entries.forEach { f ->
                            DropdownMenuItem(
                                text = { Text(t(f.labelKey, f.label)) },
                                onClick = { filterExpanded = false; vm.setFilter(f) },
                            )
                        }
                    }
                }
            }

            Box(Modifier.padding(horizontal = 10.dp, vertical = 8.dp)) {
                ScanField(
                    value = state.query,
                    onValueChange = vm::setQuery,
                    onSubmit = { },
                    placeholder = "Item code, name...",
                )
            }
            HorizontalDivider(color = Color(0xFFE2E8F0))
        }

        state.error?.let { ErrorBar(it.first, it.second) }

        Box(Modifier.weight(1f)) {
            when {
                state.loading -> LoadingBox()
                state.visible.isEmpty() && state.error == null -> EmptyBox(
                    if (state.query.isBlank()) t("htNoResults", "Không có mặt hàng nào khớp bộ lọc.")
                    else t("htNotFoundQuery", "Không tìm thấy \"{0}\".", state.query)
                )
                else -> LazyColumn(contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp)) {
                    items(state.visible, key = { it.key }) { g ->
                        val lots = t("htLotCount", "{0} lô", g.lotCount)
                        HtItemCard(
                            code = g.key,
                            subtitle = buildString {
                                append(g.name ?: "—")
                                if (g.lotCount > 1) append("  ·  ").append(lots)
                            },
                            accent = if (g.isAlert) BadgeNg else BadgeOk,
                            badge = { StockBadges(g) },
                            onClick = { onOpen(g.key) },
                        )
                    }
                }
            }
        }

        HtBottomInset()
    }
}

@Composable
private fun StockBadges(g: StockGroup) {
    Row(
        horizontalArrangement = Arrangement.spacedBy(4.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        HiPill("${fmt(g.stockQty)} ${g.unit ?: ""}".trim(), if (g.isLow) BadgeNg else BadgeOk)
        if (g.anyOnHold) HiPill(t("badgeHold", "On Hold"), BadgeNg)
        when (g.expiry) {
            ExpiryState.EXPIRED -> HiPill(t("badgeExpired", "Expired"), BadgeNg)
            ExpiryState.SOON -> HiPill(t("badgeExpSoon", "Exp. Soon"), BadgeWarn)
            ExpiryState.NONE -> Unit
        }
    }
}

@Composable
fun HiPill(text: String, bg: Color) {
    Box(
        Modifier.clip(RoundedCornerShape(10.dp)).background(bg)
            .padding(horizontal = 8.dp, vertical = 2.dp),
        contentAlignment = Alignment.Center,
    ) {
        Text(
            text,
            fontSize = 10.sp,
            fontWeight = FontWeight.SemiBold,
            color = if (bg == BadgeWarn) Color(0xFF3D2E00) else Color.White,
        )
    }
}
