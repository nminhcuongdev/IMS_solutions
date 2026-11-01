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
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.BrandBlue
import com.imssolutions.ht.ui.vm.TransferViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun TransferPickScreen(
    vm: TransferViewModel,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onNext: () -> Unit,
) {
    val state by vm.state.collectAsState()
    BusyOverlay(state.busy)
    var zoneOpen by remember { mutableStateOf(false) }
    var locOpen by remember { mutableStateOf(false) }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htTransfer", "Location Transfer"), onBack = onBack, lang = lang, onLang = onLang)

        state.error?.let { ErrorBar(it.first, it.second) }

        Column(
            Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(10.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            TfSection(t("htToLoc", "To Location")) {
                Picker(
                    label = t("htZone", "Zone"),
                    value = state.zones.firstOrNull { it.zoneCode == state.zoneCode }
                        ?.let { "${it.zoneCode} - ${it.zoneName}" },
                    expanded = zoneOpen,
                    onExpanded = { zoneOpen = it },
                    options = state.zones.map { it.zoneCode to "${it.zoneCode} - ${it.zoneName}" },
                    onPick = { zoneOpen = false; vm.selectZone(it) },
                )
                Spacer(Modifier.height(10.dp))
                Picker(
                    label = t("thLocation", "Location"),
                    value = state.locations.firstOrNull { it.locationCode == state.toLocationCode }
                        ?.let { l -> l.locationName?.let { "${l.locationCode} - $it" } ?: l.locationCode },
                    placeholder = if (state.busy) "đang tải…"
                    else if (state.locations.isEmpty()) "zone này chưa có ô kệ nào"
                    else "— chọn vị trí —",
                    expanded = locOpen,
                    onExpanded = { locOpen = it },
                    options = state.locations.map { l ->
                        l.locationCode to (l.locationName?.let { "${l.locationCode} - $it" } ?: l.locationCode)
                    },
                    onPick = { locOpen = false; vm.selectLocation(it) },
                )
            }
        }

        Row(Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface).padding(10.dp)) {
            Button(
                onClick = { if (vm.beginScan()) onNext() },
                enabled = state.canProceed && !state.busy,
                modifier = Modifier.fillMaxWidth().height(46.dp),
                shape = RoundedCornerShape(8.dp),
            ) { Text(t("htNext", "Next"), fontSize = 14.sp, fontWeight = FontWeight.SemiBold) }
        }

        HtBottomInset()
    }
}

@Composable
private fun TfSection(title: String, content: @Composable ColumnScope.() -> Unit) {
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
private fun Picker(
    label: String,
    value: String?,
    expanded: Boolean,
    onExpanded: (Boolean) -> Unit,
    options: List<Pair<String, String>>,
    onPick: (String) -> Unit,
    placeholder: String = "— chọn —",
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
                DropdownMenuItem(
                    text = { Text(text, fontSize = 13.sp) },
                    onClick = { onPick(code) },
                )
            }
        }
    }
}
