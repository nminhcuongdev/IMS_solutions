package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import com.imssolutions.ht.data.StocktakingGroup
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BadgeCancel
import com.imssolutions.ht.ui.theme.BadgeOk
import com.imssolutions.ht.ui.vm.StocktakingViewModel

@Composable
fun StocktakingListScreen(
    vm: StocktakingViewModel,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onOpen: () -> Unit,
) {
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    val snackbar = remember { SnackbarHostState() }
    val strings = LocalHtStrings.current

    LaunchedEffect(Unit) { vm.refreshGroups() }

    OnScan { raw -> vm.onScanList(raw) }

    LaunchedEffect(state.toast) {
        state.toast?.let { snackbar.showSnackbar(strings.render(it)); vm.clearToast() }
    }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(
            title = state.locationCode ?: t("htStLabel", "Stocktaking"),
            onBack = onBack, lang = lang, onLang = onLang,
        )

        state.error?.let { ErrorBar(it.first, it.second) }

        Box(Modifier.weight(1f)) {
            when {
                state.busy && state.groups.isEmpty() -> LoadingBox()
                state.groups.isEmpty() && state.error == null -> EmptyBox(
                    t("htStEmptyShelfScan", "Không còn gì để đếm ở kệ này."),
                )
                else -> LazyColumn(contentPadding = PaddingValues(horizontal = 10.dp, vertical = 8.dp)) {
                    items(state.groups, key = { it.key }) { g ->
                        GroupCard(g) { vm.open(g.key); onOpen() }
                    }
                }
            }
        }

        SnackbarHost(snackbar)
        HtBottomInset()
    }
}

@Composable
private fun GroupCard(g: StocktakingGroup, onClick: () -> Unit) {
    HtItemCard(
        code = g.itemName ?: g.itemCode,
        subtitle = listOfNotNull(
            g.itemCode.takeIf { it != (g.itemName ?: g.itemCode) },
            g.lotNo?.takeIf { it.isNotBlank() },
        ).joinToString(" · ").ifBlank { g.locationText },
        accent = if (g.counted) BadgeOk else Color.Transparent,
        badge = {
            Text(
                "${g.scannedLabels}/${g.plannedLabels}",
                style = MaterialTheme.typography.labelMedium,
                color = if (g.counted) BadgeOk else BadgeCancel,
            )
        },
        onClick = onClick,
    )
}
