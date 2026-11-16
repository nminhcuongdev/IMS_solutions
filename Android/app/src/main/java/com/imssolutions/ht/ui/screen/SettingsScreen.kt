package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Download
import androidx.compose.material.icons.filled.SystemUpdate
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.data.ServerAddress
import com.imssolutions.ht.ui.vm.SettingsViewModel

@Composable
fun SettingsScreen(
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onDownloadLabels: suspend () -> Int,
    onClearLabels: suspend () -> Unit,
    installedVersion: String = "",
    checking: Boolean = false,
    onCheckUpdate: () -> Unit = {},
) {
    val vm: SettingsViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.downloading || state.probing)
    val snackbar = remember { SnackbarHostState() }
    var confirmDownload by remember { mutableStateOf(false) }
    var confirmClear by remember { mutableStateOf(false) }

    val strings = LocalHtStrings.current
    LaunchedEffect(state.toast) {
        state.toast?.let { snackbar.showSnackbar(strings.render(it)); vm.clearToast() }
    }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htSettings", "Settings"), onBack = onBack, lang = lang, onLang = onLang)

        Column(
            Modifier.weight(1f).verticalScroll(rememberScrollState()).padding(10.dp),
        ) {
            TfSection {
                TfLabel(t("htServerIp", "Database Server IP"))
                OutlinedTextField(
                    value = state.serverIp,
                    onValueChange = vm::setServerIp,
                    singleLine = true,
                    shape = RoundedCornerShape(8.dp),
                    placeholder = { Text("192.168.1.100", fontSize = 12.sp) },
                    keyboardOptions = KeyboardOptions(keyboardType = KeyboardType.Uri),
                    isError = state.serverIp.isNotBlank() && state.serverCandidates.isEmpty(),
                    supportingText = serverHint(state.serverCandidates)?.let { h -> { Text(h, fontSize = 11.sp) } },
                    modifier = Modifier.fillMaxWidth(),
                )
                if (state.savedServers.isNotEmpty()) {
                    Spacer(Modifier.height(8.dp))
                    TfLabel(t("htSavedServers", "Saved servers"))
                    val current = state.chosenServer ?: state.serverUrl
                    state.savedServers.forEach { url ->
                        val active = current != null && ServerAddress.sameServer(url, current)
                        Row(
                            Modifier
                                .fillMaxWidth()
                                .padding(bottom = 6.dp)
                                .clip(RoundedCornerShape(8.dp))
                                .background(if (active) Color(0xFFEBF4FB) else Color(0xFFF7FAFC))
                                .border(1.dp, if (active) Color(0xFF007AC9) else Color(0xFFCBD5E0), RoundedCornerShape(8.dp))
                                .clickable { vm.pickServer(url) }
                                .padding(start = 12.dp),
                            verticalAlignment = Alignment.CenterVertically,
                        ) {
                            Text(
                                (if (active) "● " else "") + ServerAddress.toDisplay(url).let { d -> if (':' in d) d else "$d:${ServerAddress.DEFAULT_PORT}" },
                                color = if (active) Color(0xFF007AC9) else MaterialTheme.colorScheme.onSurface,
                                fontSize = 13.sp,
                                fontWeight = if (active) FontWeight.Bold else FontWeight.Normal,
                                modifier = Modifier.weight(1f).padding(vertical = 10.dp),
                            )
                            if (!active) {
                                Text(
                                    "✕",
                                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                                    fontSize = 15.sp,
                                    modifier = Modifier.clickable { vm.forgetServer(url) }.padding(horizontal = 14.dp, vertical = 10.dp),
                                )
                            }
                        }
                    }
                }
                Spacer(Modifier.height(12.dp))
                TfLabel(t("htPrinter", "Printer"))
                PickerField(
                    value = state.printers.firstOrNull { it.first == state.printerDeviceCode }?.second
                        ?: state.printerDeviceCode,
                    options = state.printers.map { it.second },
                    placeholder = t("htSelectPrinter", "Select printer"),
                    emptyText = t("htNoPrinters", "No printer with an IP address in Device Master"),
                    onPick = { label ->
                        state.printers.firstOrNull { it.second == label }?.let { vm.setPrinterDeviceCode(it.first) }
                    },
                )
            }

            Spacer(Modifier.height(10.dp))

            TfSection {
                TfLabel(t("htDefaultZoneTo", "Default Zone (To)"))
                PickerField(
                    value = state.zones.firstOrNull { it.first == state.zoneCode }?.second
                        ?: state.zoneCode,
                    options = state.zones.map { it.second },
                    placeholder = t("htZone", "Select a zone"),
                    emptyText = t("htNoPrinterDrivers", "(the list could not be loaded)"),
                    onPick = { label ->
                        state.zones.firstOrNull { it.second == label }?.let { vm.setZone(it.first) }
                    },
                )
            }

            Spacer(Modifier.height(10.dp))

            TfSection {
                Text(t("htLangDataSection", "Language Data"), fontSize = 11.sp, fontWeight = FontWeight.Bold, color = Color(0xFF007AC9))
                Spacer(Modifier.height(6.dp))
                Text(
                    state.langDataAt?.let { t("htLastUpdated", "Last updated") + ": " + it }
                        ?: t("htLangDataMissingMsg", "Language data has not been downloaded"),
                    fontSize = 11.sp,
                    color = if (state.langDataAt == null) Color(0xFFDC3545)
                            else MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Spacer(Modifier.height(10.dp))
                Button(
                    onClick = { confirmDownload = true },
                    enabled = !state.downloading,
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF5A6472)),
                    modifier = Modifier.fillMaxWidth().height(42.dp),
                ) {
                    if (state.downloading) {
                        CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp, color = Color.White)
                    } else {
                        Icon(Icons.Default.Download, null, tint = Color.White)
                        Spacer(Modifier.width(6.dp))
                        Text(t("htDataDownload", "Language Data Download"), color = Color.White, fontSize = 13.sp)
                    }
                }
                Spacer(Modifier.height(8.dp))
                Button(
                    onClick = { confirmClear = true },
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF8A8F98)),
                    modifier = Modifier.fillMaxWidth().height(42.dp),
                ) {
                    Icon(Icons.Default.Delete, null, tint = Color.White)
                    Spacer(Modifier.width(6.dp))
                    Text(t("htLangDataClear", "Language Data Clear"), color = Color.White, fontSize = 13.sp)
                }
            }

            Spacer(Modifier.height(10.dp))

            TfSection {
                Text(t("htUpdSection", "App Update"), fontSize = 11.sp, fontWeight = FontWeight.Bold, color = Color(0xFF007AC9))
                Spacer(Modifier.height(6.dp))
                TfLabel(t("htDeviceCode", "Device Code"))
                OutlinedTextField(
                    value = state.deviceId,
                    onValueChange = vm::setDeviceId,
                    singleLine = true,
                    shape = RoundedCornerShape(8.dp),
                    placeholder = { Text("HT-001", fontSize = 12.sp) },
                    supportingText = { Text(t("htDeviceCodeHint", "Device code in the Device Master"), fontSize = 11.sp) },
                    modifier = Modifier.fillMaxWidth(),
                )
                Text(
                    t("htUpdCurrent", "Current version: {0}", installedVersion),
                    fontSize = 11.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
                Spacer(Modifier.height(10.dp))
                Button(
                    onClick = { vm.save(then = onCheckUpdate) },
                    enabled = !checking,
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF5A6472)),
                    modifier = Modifier.fillMaxWidth().height(42.dp),
                ) {
                    if (checking) {
                        CircularProgressIndicator(Modifier.size(18.dp), strokeWidth = 2.dp, color = Color.White)
                    } else {
                        Icon(Icons.Default.SystemUpdate, null, tint = Color.White)
                        Spacer(Modifier.width(6.dp))
                        Text(t("htUpdCheck", "Check for updates"), color = Color.White, fontSize = 13.sp)
                    }
                }
            }

            state.error?.let {
                Spacer(Modifier.height(10.dp))
                ErrorBar(it.first, it.second)
            }
        }

        Row(
            Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface).padding(10.dp),
        ) {
            Button(
                onClick = vm::save,
                shape = RoundedCornerShape(8.dp),
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFF28A745)),
                modifier = Modifier.fillMaxWidth().height(44.dp),
            ) {
                Icon(Icons.Default.Check, null, tint = Color.White)
                Spacer(Modifier.width(6.dp))
                Text(t("btnSave", "Save"), fontWeight = FontWeight.Bold, color = Color.White)
            }
        }

        SnackbarHost(snackbar)
        HtBottomInset()
    }

    if (confirmDownload) {
        ConfirmDialog(
            title = t("htDataDownload", "Language Data Download"),
            message = "Download the language data from the server?",
            onDismiss = { confirmDownload = false },
            onOk = { confirmDownload = false; vm.downloadLanguageData(onDownloadLabels) },
        )
    }
    if (confirmClear) {
        ConfirmDialog(
            title = t("htLangDataClear", "Language Data Clear"),
            message = "Clear the language data? The LOGIN button will be locked again.",
            onDismiss = { confirmClear = false },
            onOk = { confirmClear = false; vm.clearLanguageData(onClearLabels) },
        )
    }
}

@Composable
private fun TfSection(content: @Composable ColumnScope.() -> Unit) {
    Column(
        Modifier
            .fillMaxWidth()
            .background(MaterialTheme.colorScheme.surface, RoundedCornerShape(10.dp))
            .padding(12.dp),
        content = content,
    )
}

@Composable
private fun TfLabel(text: String) {
    Text(
        text,
        fontSize = 12.sp,
        fontWeight = FontWeight.SemiBold,
        color = MaterialTheme.colorScheme.onSurfaceVariant,
        modifier = Modifier.padding(bottom = 4.dp),
    )
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun PickerField(
    value: String,
    options: List<String>,
    placeholder: String,
    emptyText: String,
    onPick: (String) -> Unit,
) {
    var expanded by remember { mutableStateOf(false) }
    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = it }) {
        OutlinedTextField(
            value = value,
            onValueChange = {},
            readOnly = true,
            singleLine = true,
            placeholder = { Text(placeholder, fontSize = 13.sp) },
            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
            shape = RoundedCornerShape(8.dp),
            modifier = Modifier.menuAnchor(MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
        )
        ExposedDropdownMenu(expanded, onDismissRequest = { expanded = false }) {
            if (options.isEmpty()) {
                DropdownMenuItem(
                    text = { Text(emptyText, fontSize = 12.sp) },
                    onClick = { expanded = false },
                )
            }
            options.forEach { o ->
                DropdownMenuItem(text = { Text(o) }, onClick = { expanded = false; onPick(o) })
            }
        }
    }
}

@Composable
private fun ConfirmDialog(title: String, message: String, onDismiss: () -> Unit, onOk: () -> Unit) {
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(title) },
        text = { Text(message) },
        confirmButton = { Button(onClick = onOk) { Text(t("thYes", "OK")) } },
        dismissButton = { TextButton(onClick = onDismiss) { Text(t("htClose", "CANCEL")) } },
    )
}

@Composable
private fun serverHint(candidates: List<String>): String? = when {
    candidates.isEmpty() -> null
    candidates.size == 1 -> candidates.first()
    else -> t(
        "htPortAuto",
        "{0} — port auto-detect: {1}",
        candidates.first().removePrefix("http://").substringBefore(':'),
        candidates.joinToString(", ") { it.trimEnd('/').substringAfterLast(':') },
    )
}
