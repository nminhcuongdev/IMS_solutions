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
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.data.ExpiryState
import com.imssolutions.ht.data.StockLine
import com.imssolutions.ht.data.expiryStateOf
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BadgeNg
import com.imssolutions.ht.ui.theme.BadgeWarn
import com.imssolutions.ht.ui.theme.BrandBlue
import com.imssolutions.ht.ui.vm.StockDetailViewModel
import com.imssolutions.ht.ui.vm.fmt

@Composable
fun StockDetailScreen(
    itemCode: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
) {
    val vm: StockDetailViewModel = viewModel()
    val state by vm.state.collectAsState()

    LaunchedEffect(itemCode) { vm.load(itemCode) }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htInvDetail", "Stock Detail"), onBack = onBack, lang = lang, onLang = onLang)

        state.error?.let { ErrorBar(it.first, it.second) }

        Box(Modifier.weight(1f)) {
            when {
                state.loading -> LoadingBox()
                state.empty -> EmptyBox(t("htNoStockForItem", "Mặt hàng \"{0}\" không còn tồn kho.", itemCode))
                else -> Column(
                    Modifier.fillMaxSize().verticalScroll(rememberScrollState())
                        .padding(horizontal = 10.dp, vertical = 8.dp),
                    verticalArrangement = Arrangement.spacedBy(8.dp),
                ) {
                    if (state.anyOnHold) {
                        HoldBanner(state.holdReason)
                    }

                    val head = state.head
                    StockCard(t("htItemInfo", "Item Information")) {
                        SRow(t("thItemCode", "Item Code"), head?.itemCode ?: itemCode)
                        SRow(t("thItemName", "Item Name"), head?.itemName ?: "—")
                        SRow(t("thCategory", "Category"), head?.category ?: "—")
                        SRow(t("thWarehouse", "Warehouse"), head?.warehouse ?: "—")
                    }

                    StockCard(t("htStockInfo", "Stock Information")) {
                        StockTableHead()
                        state.lines.forEach { StockTableRow(it) }
                        HorizontalDivider(color = Color(0xFFE2E8F0))
                        Row(
                            Modifier.fillMaxWidth().padding(top = 6.dp),
                            horizontalArrangement = Arrangement.SpaceBetween,
                        ) {
                            Text(t("htTotal", "Total"), fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                            Text(
                                "${fmt(state.totalQty)} ${state.head?.unit ?: ""}".trim(),
                                fontSize = 12.5.sp,
                                fontWeight = FontWeight.Bold,
                                color = BrandBlue,
                            )
                        }
                    }
                }
            }
        }

        HtBottomInset()
    }
}

@Composable
private fun HoldBanner(reason: String?) {
    Row(
        Modifier.fillMaxWidth()
            .background(BadgeNg.copy(alpha = 0.12f), RoundedCornerShape(8.dp))
            .padding(horizontal = 12.dp, vertical = 10.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(
            buildString {
                append("This inventory is on HOLD.")
                reason?.let { append("  (").append(it).append(")") }
            },
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            color = BadgeNg,
        )
    }
}

@Composable
private fun StockCard(title: String, content: @Composable ColumnScope.() -> Unit) {
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
private fun SRow(label: String, value: String) {
    Row(
        Modifier.fillMaxWidth().padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(label, fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
        Text(value, fontSize = 12.5.sp, fontWeight = FontWeight.SemiBold)
    }
}

@Composable
private fun StockTableHead() {
    Row(Modifier.fillMaxWidth().padding(bottom = 4.dp)) {
        THCell(t("thLocation", "Location"), 2.2f)
        THCell(t("thLot", "Lot"), 1.8f)
        THCell(t("thQty", "Qty"), 1f, TextAlign.End)
        THCell(t("thUnit", "Unit"), 0.9f, pad = 6.dp)
    }
    HorizontalDivider(color = Color(0xFFE2E8F0))
}

@Composable
private fun RowScope.THCell(
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
private fun StockTableRow(l: StockLine) {
    val exp = expiryStateOf(l.remainingDays)
    val tint = when {
        l.isOnHold || exp == ExpiryState.EXPIRED -> BadgeNg
        exp == ExpiryState.SOON -> Color(0xFF8A6D00)
        else -> MaterialTheme.colorScheme.onSurface
    }
    Row(
        Modifier.fillMaxWidth().padding(vertical = 5.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(l.locationCode ?: "—", Modifier.weight(2.2f), fontSize = 11.5.sp, color = tint)
        Text(l.lotNo ?: "—", Modifier.weight(1.8f), fontSize = 11.5.sp, color = tint)
        Text(
            fmt(l.stockQty), Modifier.weight(1f),
            fontSize = 11.5.sp, fontWeight = FontWeight.SemiBold,
            textAlign = TextAlign.End, color = tint,
        )
        Text(
            l.unit ?: "—", Modifier.weight(0.9f).padding(start = 6.dp),
            fontSize = 11.5.sp, color = tint,
        )
    }
    if (l.isOnHold || exp != ExpiryState.NONE) {
        Row(
            Modifier.fillMaxWidth().padding(bottom = 4.dp),
            horizontalArrangement = Arrangement.spacedBy(4.dp),
        ) {
            if (l.isOnHold) HiPill(t("badgeHold", "On Hold"), BadgeNg)
            when (exp) {
                ExpiryState.EXPIRED -> HiPill(t("badgeExpired", "Expired"), BadgeNg)
                ExpiryState.SOON -> HiPill(t("badgeExpSoon", "Exp. Soon"), BadgeWarn)
                ExpiryState.NONE -> Unit
            }
        }
    }
}
