package com.imssolutions.ht.ui.screen

import com.imssolutions.ht.ui.t
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.KeyboardArrowRight
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.launch
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.data.productionMenuKeys
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.HtBottomInset

private val MENU = listOf(
    MenuEntry("ht_receiving", "htReceiving", "Receiving", enabled = true),
    MenuEntry("ht_returns", "htReturns", "Returns", enabled = true),
    MenuEntry("ht_picking", "htPicking", "Picking", enabled = true),
    MenuEntry("ht_production", "hpProduction", "Production", enabled = true),
    MenuEntry("ht_shipping", "htShipping", "Shipping", enabled = true),
    MenuEntry("ht_inventory", "htInventory", "Stock Inquiry", enabled = true),
    MenuEntry("ht_transfer", "htTransfer", "Location Transfer", enabled = true),
    MenuEntry("ht_stocktaking", "htStLabel", "Stocktaking", enabled = true),
    MenuEntry("ht_disposal", "htDisposal", "Disposal", enabled = true),
)

@Composable
fun MenuScreen(
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onReceiving: () -> Unit,
    onReturns: () -> Unit,
    onPicking: () -> Unit,
    onShipping: () -> Unit,
    onInventory: () -> Unit,
    onDisposal: () -> Unit,
    onTransfer: () -> Unit,
    onStocktaking: () -> Unit,
    onProduction: (String) -> Unit,
    onLoggedOut: () -> Unit,
    pendingUpdate: String? = null,
    onOpenUpdate: () -> Unit = {},
) {
    val scope = rememberCoroutineScope()
    val session by ApiClient.tokens.session.collectAsState(initial = null)
    var confirming by remember { mutableStateOf(false) }
    var productionOpen by remember { mutableStateOf(false) }
    var productionKeys by remember { mutableStateOf(emptySet<String>()) }

    LaunchedEffect(session?.userCode) {
        productionKeys = emptySet()
        if (session != null) productionKeys = runCatching {
            productionMenuKeys(ApiClient.call { myPermissions() }.data)
        }.getOrDefault(emptySet())
    }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = if (productionOpen) t("hpProduction", "Production") else t("htMenu", "Menu"),
            onBack = if (productionOpen) { { productionOpen = false } } else null, lang = lang, onLang = onLang)

        Column(
            Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            if (pendingUpdate != null && !productionOpen) {
                Row(
                    Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(12.dp))
                        .background(Color(0xFFFFF3CD))
                        .clickable(onClick = onOpenUpdate)
                        .padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(
                        t("htUpdPending", "Có phiên bản mới {0}. Chạm để cập nhật.", pendingUpdate),
                        fontSize = 13.sp,
                        fontWeight = FontWeight.SemiBold,
                        color = Color(0xFF856404),
                        modifier = Modifier.weight(1f),
                    )
                }
            }
            val entries = if (productionOpen) listOf(
                MenuEntry("ht_prd_receipt", "hpMatReceipt", "Material Receipt", true),
                MenuEntry("ht_prd_input", "hpMatInput", "Material Input", true),
                MenuEntry("ht_prd_return", "hpMatReturn", "Material Return", true),
                MenuEntry("ht_prd_output", "hpProdComplete", "Production Completion", true),
            ).filter { it.navKey in productionKeys } else MENU.filter { it.navKey != "ht_production" || productionKeys.isNotEmpty() }
            entries.forEach { e ->
                MenuItemRow(e) {
                    when (e.navKey) {
                        "ht_production" -> productionOpen = true
                        "ht_prd_receipt" -> onProduction("receipt")
                        "ht_prd_input" -> onProduction("input")
                        "ht_prd_return" -> onProduction("return")
                        "ht_prd_output" -> onProduction("output")
                        "ht_receiving" -> onReceiving()
                        "ht_returns" -> onReturns()
                        "ht_picking" -> onPicking()
                        "ht_shipping" -> onShipping()
                        "ht_inventory" -> onInventory()
                        "ht_disposal" -> onDisposal()
                        "ht_transfer" -> onTransfer()
                        "ht_stocktaking" -> onStocktaking()
                    }
                }
            }

            Spacer(Modifier.height(4.dp))
            Row(
                Modifier
                    .fillMaxWidth()
                    .clip(RoundedCornerShape(12.dp))
                    .background(MaterialTheme.colorScheme.surface)
                    .padding(12.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(Modifier.weight(1f)) {
                    Text(
                        session?.userName ?: "—",
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Bold,
                    )
                    Text(
                        session?.userCode ?: "",
                        fontSize = 11.sp,
                        color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                }
                TextButton(onClick = { confirming = true }) {
                    Text(t("logout", "Logout"), color = MaterialTheme.colorScheme.error, fontSize = 13.sp)
                }
            }
        }

        HtBottomInset()
    }

    if (confirming) {
        AlertDialog(
            onDismissRequest = { confirming = false },
            title = { Text(t("logout", "Đăng xuất") + "?") },
            text = { Text(t("htLogoutConfirm", "Bạn sẽ phải nhập lại User ID và mật khẩu.")) },
            confirmButton = {
                Button(onClick = {
                    confirming = false
                    scope.launch {
                        runCatching { ApiClient.call { logout() } }
                        ApiClient.tokens.clear()
                        onLoggedOut()
                    }
                }) { Text(t("logout", "Đăng xuất")) }
            },
            dismissButton = { TextButton(onClick = { confirming = false }) { Text(t("htCancel", "Huỷ")) } },
        )
    }
}

@Composable
private fun MenuItemRow(e: MenuEntry, onClick: () -> Unit) {
    Row(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(12.dp))
            .background(MaterialTheme.colorScheme.surface)
            .then(if (e.enabled) Modifier.clickable(onClick = onClick) else Modifier)
            .alpha(if (e.enabled) 1f else 0.45f)
            .padding(16.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(t(e.labelKey, e.title), fontSize = 14.sp, fontWeight = FontWeight.Bold)
            if (!e.enabled) {
                Text(
                    t("htNotImplemented", "Chưa phát triển"),
                    fontSize = 10.5.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }
        Icon(
            Icons.AutoMirrored.Filled.KeyboardArrowRight,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}
