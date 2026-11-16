package com.imssolutions.ht.ui.screen

import com.imssolutions.ht.ui.t
import androidx.activity.compose.BackHandler
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
import androidx.compose.runtime.saveable.rememberSaveable
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
import com.imssolutions.ht.data.HT_CATEGORIES
import com.imssolutions.ht.data.HtMenuAccess
import com.imssolutions.ht.data.htCategoryEnabled
import com.imssolutions.ht.data.htCategoryFeatures
import com.imssolutions.ht.data.htCategoryVisible
import com.imssolutions.ht.data.htMenuAccess
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.HtBottomInset

private val FEATURES = mapOf(
    "ht_receiving" to ("htReceiving" to "Receiving"),
    "ht_returns" to ("htReturns" to "Returns"),
    "ht_picking" to ("htPicking" to "Picking"),
    "ht_shipping" to ("htShipping" to "Shipping"),
    "ht_inventory" to ("htInventory" to "Stock Inquiry"),
    "ht_transfer" to ("htTransfer" to "Location Transfer"),
    "ht_prd_receipt" to ("hpMatReceipt" to "Material Receipt"),
    "ht_prd_input" to ("hpMatInput" to "Material Input"),
    "ht_prd_return" to ("hpMatReturn" to "Material Return"),
    "ht_prd_output" to ("hpProdComplete" to "Production Completion"),
    "ht_disposal" to ("htDisposal" to "Disposal"),
    "ht_stocktaking" to ("htStLabel" to "Stocktaking"),
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
    var categoryKey by rememberSaveable { mutableStateOf<String?>(null) }
    var access by remember { mutableStateOf(HtMenuAccess(emptySet(), emptySet())) }
    var loadFailed by remember { mutableStateOf(false) }
    var reload by remember { mutableIntStateOf(0) }
    val category = HT_CATEGORIES.firstOrNull { it.key == categoryKey }

    LaunchedEffect(session?.userCode, categoryKey, reload) {
        if (session == null) return@LaunchedEffect
        runCatching { ApiClient.call { myPermissions() }.data }
            .onSuccess { access = htMenuAccess(it); loadFailed = false }
            .onFailure { access = HtMenuAccess(emptySet(), access.visible); loadFailed = true }
    }

    BackHandler(enabled = category != null) { categoryKey = null }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(
            title = category?.let { t(it.labelKey, it.title) } ?: t("htMenu", "Menu"),
            onBack = if (category != null) { { categoryKey = null } } else null,
            lang = lang,
            onLang = onLang,
        )

        Column(
            Modifier
                .weight(1f)
                .verticalScroll(rememberScrollState())
                .padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            if (pendingUpdate != null && category == null) {
                Banner(t("htUpdPending", "Version {0} is available. Tap to update.", pendingUpdate), onOpenUpdate)
            }
            if (loadFailed) {
                Banner(t("htPermLoadFailed", "Permissions could not be loaded. Tap to retry."), { reload++ })
            }
            if (category == null) {
                HT_CATEGORIES.filter { htCategoryVisible(it, access) || loadFailed }.forEach { c ->
                    MenuItemRow(t(c.labelKey, c.title), enabled = htCategoryEnabled(c, access)) { categoryKey = c.key }
                }
            } else {
                htCategoryFeatures(category, access).ifEmpty { if (loadFailed) category.features else emptyList() }.forEach { code ->
                    val (key, title) = FEATURES.getValue(code)
                    MenuItemRow(t(key, title), enabled = code in access.allowed) {
                        when (code) {
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
            }
        }

        if (category == null) {
            Row(
                Modifier
                    .fillMaxWidth()
                    .background(MaterialTheme.colorScheme.surface)
                    .padding(horizontal = 14.dp, vertical = 10.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(Modifier.weight(1f)) {
                    Text(session?.userName ?: "—", fontSize = 13.sp, fontWeight = FontWeight.Bold)
                    Text(session?.userCode ?: "", fontSize = 11.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
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
            title = { Text(t("logout", "Logout") + "?") },
            text = { Text(t("htLogoutConfirm", "You will have to enter your User ID and password again.")) },
            confirmButton = {
                Button(onClick = {
                    confirming = false
                    scope.launch {
                        runCatching { ApiClient.call { logout() } }
                        ApiClient.tokens.clear()
                        onLoggedOut()
                    }
                }) { Text(t("logout", "Logout")) }
            },
            dismissButton = { TextButton(onClick = { confirming = false }) { Text(t("htCancel", "Cancel")) } },
        )
    }
}

@Composable
private fun Banner(text: String, onClick: () -> Unit) {
    Row(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(12.dp))
            .background(Color(0xFFFFF3CD))
            .clickable(onClick = onClick)
            .padding(12.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(text, fontSize = 13.sp, fontWeight = FontWeight.SemiBold, color = Color(0xFF856404), modifier = Modifier.weight(1f))
    }
}

@Composable
private fun MenuItemRow(title: String, enabled: Boolean, onClick: () -> Unit) {
    Row(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(12.dp))
            .background(MaterialTheme.colorScheme.surface)
            .then(if (enabled) Modifier.clickable(onClick = onClick) else Modifier)
            .alpha(if (enabled) 1f else 0.45f)
            .padding(16.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Column(Modifier.weight(1f)) {
            Text(title, fontSize = 14.sp, fontWeight = FontWeight.Bold)
            if (!enabled) {
                Text(t("htNoPermission", "No permission"), fontSize = 10.5.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }
        Icon(
            Icons.AutoMirrored.Filled.KeyboardArrowRight,
            contentDescription = null,
            tint = MaterialTheme.colorScheme.onSurfaceVariant,
        )
    }
}
