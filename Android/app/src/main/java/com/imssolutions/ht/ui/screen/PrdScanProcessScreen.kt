package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.MenuAnchorType
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.scan.QrLabelKind
import com.imssolutions.ht.scan.QrPayload
import com.imssolutions.ht.scan.QrScan
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtBottomInset
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.LoadingBox
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.vm.PrdOrderViewModel

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PrdScanProcessScreen(
    orderNo: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onAccepted: (String) -> Unit,
) {
    val vm: PrdOrderViewModel = viewModel()
    val state by vm.state.collectAsState()
    var qr by remember { mutableStateOf("") }
    var expanded by remember { mutableStateOf(false) }

    LaunchedEffect(Unit) { vm.load(orderNo) }

    fun accept(code: String) {
        val error = vm.processError(code)
        if (error == null) onAccepted(code)
        else vm.showError(error, if (error == "SYS-404") code else error)
    }

    fun submit(raw: String) {
        val payload = (QrPayload.parse(raw) as? QrScan.Ok)?.payload
        val code = payload?.processCode
        if (payload == null || payload.kind != QrLabelKind.Process || code.isNullOrBlank()) {
            vm.showError("SCN-001", "The scanned barcode is invalid.")
            return
        }
        accept(code)
    }

    OnScan { raw -> qr = raw; submit(raw) }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("hpScanProc", "Scan Process QR"), onBack = onBack, lang = lang, onLang = onLang)

        Box(Modifier.weight(1f)) {
            if (state.loading) LoadingBox()
            else Column(Modifier.fillMaxSize().verticalScroll(rememberScrollState()).padding(10.dp)) {
                PrdCard(t("hpScanProcHead", "Process label")) {
                    PrdScanMsg(t("hpScanProcMsg", "Scan the process QR (?pr_cd=...) at the line."))
                    PrdField(t("hpQrText", "QR text")) {
                        Row(verticalAlignment = Alignment.CenterVertically, horizontalArrangement = Arrangement.spacedBy(6.dp)) {
                            OutlinedTextField(
                                value = qr,
                                onValueChange = { qr = it },
                                singleLine = true,
                                shape = RoundedCornerShape(8.dp),
                                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Done),
                                keyboardActions = KeyboardActions(onDone = { submit(qr) }),
                                modifier = Modifier.weight(1f),
                            )
                            Button(onClick = { submit(qr) }, shape = RoundedCornerShape(8.dp), modifier = Modifier.height(52.dp)) {
                                Text("OK")
                            }
                        }
                    }
                }
                PrdCard(t("hpPickProc", "If the QR cannot be read")) {
                    ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = it }) {
                        OutlinedTextField(
                            value = "",
                            onValueChange = {},
                            readOnly = true,
                            singleLine = true,
                            placeholder = { Text(t("hpSelectProc", "Select process..."), fontSize = 13.sp) },
                            trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
                            shape = RoundedCornerShape(8.dp),
                            modifier = Modifier.menuAnchor(MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
                        )
                        ExposedDropdownMenu(expanded, onDismissRequest = { expanded = false }) {
                            state.processes.forEach { line ->
                                DropdownMenuItem(
                                    text = { Text("${line.processName ?: line.processCode} (${line.processCode})") },
                                    onClick = { expanded = false; accept(line.processCode) },
                                )
                            }
                        }
                    }
                }
            }
        }
        HtBottomInset()
    }

    PrdErrorDialog(state.error) { vm.clearError() }
}
