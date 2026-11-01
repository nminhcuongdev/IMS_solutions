package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BrandBlue
import com.imssolutions.ht.ui.vm.StocktakingViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun StocktakingLocationScreen(
    vm: StocktakingViewModel,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onNext: () -> Unit,
) {
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    var zoneOpen by remember { mutableStateOf(false) }
    var locOpen by remember { mutableStateOf(false) }

    OnScan { raw -> vm.onScanLocation(raw, onNext) }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htStLabel", "Stocktaking"), onBack = onBack, lang = lang, onLang = onLang)

        state.error?.let { ErrorBar(it.first, it.second) }

        Column(
            Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(10.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            state.session?.let { s ->
                Row(
                    Modifier.fillMaxWidth()
                        .background(MaterialTheme.colorScheme.surface, RoundedCornerShape(10.dp))
                        .padding(horizontal = 12.dp, vertical = 10.dp),
                    horizontalArrangement = Arrangement.SpaceBetween,
                ) {
                    Text(
                        t("stName", "Stocktaking Name"),
                        fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    Text(s.sessionName, fontSize = 13.sp, fontWeight = FontWeight.Bold, color = BrandBlue)
                }
            }

            if (state.open) {
                StSection(t("htSelectLocation", "Select Location")) {
                    Text(
                        t("htStScanOrPickLocation", "Quét tem vị trí hoặc chọn bên dưới."),
                        fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant,
                    )
                    Spacer(Modifier.height(8.dp))
                    StPicker(
                        label = t("htArea", "Area"),
                        value = state.zones.firstOrNull { it.zoneCode == state.zoneCode }
                            ?.let { "${it.zoneCode} - ${it.zoneName}" },
                        expanded = zoneOpen,
                        onExpanded = { zoneOpen = it },
                        options = state.zones.map { it.zoneCode to "${it.zoneCode} - ${it.zoneName}" },
                        onPick = { zoneOpen = false; vm.selectZone(it) },
                    )
                    Spacer(Modifier.height(10.dp))
                    StPicker(
                        label = t("thLocation", "Location"),
                        value = state.locations.firstOrNull { it.locationCode == state.locationCode }
                            ?.let { l -> l.locationName?.let { "${l.locationCode} - $it" } ?: l.locationCode }
                            ?: state.locationCode,
                        placeholder = if (state.busy) t("htLoading", "Đang tải…")
                        else if (state.locations.isEmpty()) t("htNoLocationsInZone", "Khu vực này chưa có ô kệ nào.")
                        else t("htPickLocation", "— Chọn vị trí —"),
                        expanded = locOpen,
                        onExpanded = { locOpen = it },
                        options = state.locations.map { l ->
                            l.locationCode to (l.locationName?.let { "${l.locationCode} - $it" } ?: l.locationCode)
                        },
                        onPick = { locOpen = false; vm.selectLocation(it) },
                    )
                }
            }
        }

        Row(Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface).padding(10.dp)) {
            Button(
                onClick = { if (state.open) vm.openLocation(onNext) else vm.load() },
                enabled = !state.busy && (state.canProceed || !state.open),
                modifier = Modifier.fillMaxWidth().height(46.dp),
                shape = RoundedCornerShape(8.dp),
            ) {
                if (state.busy) {
                    CircularProgressIndicator(
                        Modifier.size(18.dp), strokeWidth = 2.dp,
                        color = MaterialTheme.colorScheme.onPrimary,
                    )
                } else {
                    Text(
                        if (state.open) t("htNext", "Next") else t("btnRetry", "Retry"),
                        fontSize = 14.sp, fontWeight = FontWeight.SemiBold,
                    )
                }
            }
        }

        HtBottomInset()
    }
}

@Composable
private fun StSection(title: String, content: @Composable ColumnScope.() -> Unit) {
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

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun StPicker(
    label: String,
    value: String?,
    expanded: Boolean,
    onExpanded: (Boolean) -> Unit,
    options: List<Pair<String, String>>,
    onPick: (String) -> Unit,
    placeholder: String = t("htPickOne", "— Chọn —"),
) {
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = onExpanded) {
        OutlinedTextField(
            value = value ?: placeholder,
            onValueChange = {},
            readOnly = true,
            singleLine = true,
            label = { Text(label, fontSize = 11.sp) },
            isError = value == null,
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
            shape = RoundedCornerShape(8.dp),
            modifier = Modifier.menuAnchor(MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
        )
        ExposedDropdownMenu(expanded, onDismissRequest = { onExpanded(false) }) {
            options.forEach { (code, text) ->
                DropdownMenuItem(text = { Text(text, fontSize = 13.sp) }, onClick = { onPick(code) })
            }
        }
    }
}
