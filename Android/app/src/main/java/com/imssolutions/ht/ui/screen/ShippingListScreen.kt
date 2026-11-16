package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Close
import androidx.compose.material.icons.filled.DateRange
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.*
import com.imssolutions.ht.ui.theme.statusColor
import com.imssolutions.ht.ui.vm.ShippingListViewModel
import com.imssolutions.ht.ui.vm.ShpFilter

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ShippingListScreen(
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onOpen: (String) -> Unit,
) {
    val vm: ShippingListViewModel = viewModel()
    val state by vm.state.collectAsState()
    var statusExpanded by remember { mutableStateOf(false) }
    val context = LocalContext.current

    LaunchedEffect(Unit) { vm.load() }

    OnScan { code -> vm.setQuery(code) }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = t("htShipping", "Shipping"), onBack = onBack, lang = lang, onLang = onLang)

        Column(Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface)) {
            Row(
                Modifier.fillMaxWidth().padding(start = 10.dp, end = 10.dp, top = 8.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                OutlinedTextField(
                    value = state.date ?: "",
                    onValueChange = {},
                    readOnly = true,
                    singleLine = true,
                    placeholder = { Text(t("htAnyDate", "Any date"), fontSize = 13.sp) },
                    leadingIcon = { Icon(Icons.Default.DateRange, contentDescription = null) },
                    trailingIcon = {
                        if (state.date != null) {
                            IconButton(onClick = { vm.setDate(null) }) {
                                Icon(Icons.Default.Close, contentDescription = t("htClearDateFilter", "Clear date filter"))
                            }
                        }
                    },
                    shape = RoundedCornerShape(8.dp),
                    modifier = Modifier
                        .weight(1f)
                        .clickable { showShippingDatePicker(context, state.date) { vm.setDate(it) } },
                )
            }

            Box(Modifier.padding(start = 10.dp, end = 10.dp, top = 8.dp)) {
                ExposedDropdownMenuBox(
                    expanded = statusExpanded,
                    onExpandedChange = { statusExpanded = it },
                ) {
                    OutlinedTextField(
                        value = t(state.filter.labelKey, state.filter.label),
                        onValueChange = {},
                        readOnly = true,
                        singleLine = true,
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(statusExpanded) },
                        shape = RoundedCornerShape(8.dp),
                        modifier = Modifier
                            .menuAnchor(MenuAnchorType.PrimaryNotEditable)
                            .fillMaxWidth(),
                    )
                    ExposedDropdownMenu(statusExpanded, onDismissRequest = { statusExpanded = false }) {
                        ShpFilter.entries.forEach { f ->
                            DropdownMenuItem(
                                text = { Text(t(f.labelKey, f.label)) },
                                onClick = { statusExpanded = false; vm.setFilter(f) },
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
                    placeholder = t("htSlipSearch", "Slip number..."),
                )
            }
            HorizontalDivider(color = Color(0xFFE2E8F0))
        }

        state.error?.let { ErrorBar(it.first, it.second) }

        Box(Modifier.weight(1f)) {
            when {
                state.loading -> LoadingBox()
                state.slips.isEmpty() && state.error == null -> EmptyBox(
                    if (state.query.isBlank()) t("htNoResults", "No shipping slips match the filter.")
                    else t("htNotFoundQuery", "\"{0}\" was not found.", state.query)
                )
                else -> LazyColumn(contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp)) {
                    items(state.slips, key = { it.slipNo }) { slip ->
                        HtItemCard(
                            code = slip.slipNo,
                            subtitle = listOfNotNull(
                                slip.customerName,
                                slip.gateCode?.let { "Gate $it" },
                            ).joinToString(" · ").ifBlank { "—" },
                            accent = statusColor(slip.status),
                            badge = { HiBadge(slip.status) },
                            onClick = { onOpen(slip.slipNo) },
                        )
                    }
                }
            }
        }

        HtBottomInset()
    }
}

private fun showShippingDatePicker(
    context: android.content.Context,
    current: String?,
    onPick: (String) -> Unit,
) {
    val c = java.util.Calendar.getInstance()
    current?.split("-")?.takeIf { it.size == 3 }?.let { (y, m, d) ->
        c.set(y.toInt(), m.toInt() - 1, d.toInt())
    }
    android.app.DatePickerDialog(
        context,
        { _, y, m, d -> onPick("%04d-%02d-%02d".format(y, m + 1, d)) },
        c.get(java.util.Calendar.YEAR),
        c.get(java.util.Calendar.MONTH),
        c.get(java.util.Calendar.DAY_OF_MONTH),
    ).show()
}
