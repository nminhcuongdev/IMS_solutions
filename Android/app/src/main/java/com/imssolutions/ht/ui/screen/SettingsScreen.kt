package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.Download
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.vm.SettingsViewModel

@Composable
fun SettingsScreen(
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onDownloadLabels: suspend () -> Int,
    onClearLabels: suspend () -> Unit,
) {
    val vm: SettingsViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.downloading)
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
                    placeholder = { Text("http://192.168.1.100:8091/", fontSize = 12.sp) },
                    modifier = Modifier.fillMaxWidth(),
                )
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
                    placeholder = t("htZone", "Chọn zone"),
                    emptyText = t("htNoPrinterDrivers", "(không lấy được danh sách)"),
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
                    state.langDataAt?.let { t("htLastUpdated", "Cập nhật lần cuối") + ": " + it }
                        ?: t("htLangDataMissingMsg", "Chưa tải dữ liệu ngôn ngữ"),
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
        HtStatusBar(clock = nowClock(), right = t("htSettings", "Settings"))
    }

    if (confirmDownload) {
        ConfirmDialog(
            title = t("htDataDownload", "Language Data Download"),
            message = "Tải dữ liệu ngôn ngữ từ máy chủ?",
            onDismiss = { confirmDownload = false },
            onOk = { confirmDownload = false; vm.downloadLanguageData(onDownloadLabels) },
        )
    }
    if (confirmClear) {
        ConfirmDialog(
            title = t("htLangDataClear", "Language Data Clear"),
            message = "Xoá mốc dữ liệu ngôn ngữ? Nút LOGIN sẽ bị khoá lại.",
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
