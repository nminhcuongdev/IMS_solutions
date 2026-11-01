package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.itemsIndexed
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.ExposedDropdownMenuBox
import androidx.compose.material3.ExposedDropdownMenuDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
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
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.unit.dp
import androidx.lifecycle.viewmodel.compose.viewModel
import com.imssolutions.ht.scan.OnScan
import com.imssolutions.ht.ui.EmptyBox
import com.imssolutions.ht.ui.ErrorBar
import com.imssolutions.ht.ui.HtBodyBg
import com.imssolutions.ht.ui.HtBottomInset
import com.imssolutions.ht.ui.HtHeader
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.LoadingBox
import com.imssolutions.ht.ui.ScanField
import com.imssolutions.ht.ui.distinctKeys
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.theme.BadgeInfo
import com.imssolutions.ht.ui.theme.BadgeOk
import com.imssolutions.ht.ui.vm.PrdListViewModel
import com.imssolutions.ht.ui.vm.fmt

private val FILTERS = listOf(
    Triple("active", "htFilterActive", "Not Completed"),
    Triple("all", "htFilterAll", "All"),
    Triple("completed", "htFilterCompleted", "Completed"),
)

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun PrdOrderListScreen(
    kind: String,
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onBack: () -> Unit,
    onOpen: (String) -> Unit,
) {
    val vm: PrdListViewModel = viewModel()
    val state by vm.state.collectAsState()
    var expanded by remember { mutableStateOf(false) }

    LaunchedEffect(kind) { vm.start(kind) }
    OnScan { code -> vm.setQuery(code) }

    val title = when (kind) {
        "receipt" -> t("hpMatReceipt", "Material Receipt")
        "input" -> t("hpMatInput", "Material Input")
        "return" -> t("hpMatReturn", "Material Return")
        else -> t("hpProdComplete", "Production Completion")
    }

    Column(Modifier.fillMaxSize().background(HtBodyBg)) {
        HtHeader(title = title, onBack = onBack, lang = lang, onLang = onLang)

        Column(Modifier.fillMaxWidth().background(MaterialTheme.colorScheme.surface)) {
            Box(Modifier.padding(start = 10.dp, end = 10.dp, top = 8.dp)) {
                ExposedDropdownMenuBox(expanded = expanded, onExpandedChange = { expanded = it }) {
                    val current = FILTERS.first { it.first == state.filter }
                    OutlinedTextField(
                        value = t(current.second, current.third),
                        onValueChange = {},
                        readOnly = true,
                        singleLine = true,
                        trailingIcon = { ExposedDropdownMenuDefaults.TrailingIcon(expanded) },
                        shape = RoundedCornerShape(8.dp),
                        modifier = Modifier.menuAnchor(MenuAnchorType.PrimaryNotEditable).fillMaxWidth(),
                    )
                    ExposedDropdownMenu(expanded, onDismissRequest = { expanded = false }) {
                        FILTERS.forEach { (value, key, fallback) ->
                            DropdownMenuItem(
                                text = { Text(t(key, fallback)) },
                                onClick = { expanded = false; vm.setFilter(value) },
                            )
                        }
                    }
                }
            }
            Box(Modifier.padding(horizontal = 10.dp, vertical = 8.dp)) {
                ScanField(
                    value = state.query,
                    onValueChange = vm::setQuery,
                    onSubmit = {},
                    placeholder = t("hpOrderSearch", "Order No..."),
                )
            }
            HorizontalDivider(color = Color(0xFFE2E8F0))
        }

        state.error?.let { ErrorBar(it.first, it.second) }

        Box(Modifier.weight(1f)) {
            val rows = vm.visible(kind)
            when {
                state.loading -> LoadingBox()
                rows.isEmpty() && state.error == null -> EmptyBox(t("hpNoOrders", "No target production orders."))
                else -> LazyColumn(contentPadding = PaddingValues(horizontal = 10.dp, vertical = 6.dp)) {
                    val keys = distinctKeys(rows.map { it.productionOrderNo })
                    itemsIndexed(rows, key = { i, _ -> keys[i] }) { _, order ->
                        val received = state.lines.any { it.productionOrderNo == order.productionOrderNo && it.received }
                        when (kind) {
                            "receipt" -> PrdOrderCard(
                                order,
                                extraBadge = {
                                    if (received) PrdBadge(t("hpReceivedBadge", "Received"), BadgeOk)
                                    else PrdBadge(t("hpNotReceived", "Not received"), BadgeInfo)
                                },
                                onClick = { onOpen(order.productionOrderNo) },
                            )
                            "output" -> PrdOrderCard(
                                order,
                                meta = "${t("hpProduced", "Produced")}: ${fmt(order.outputQty)}",
                                onClick = { onOpen(order.productionOrderNo) },
                            )
                            else -> PrdOrderCard(order, onClick = { onOpen(order.productionOrderNo) })
                        }
                    }
                }
            }
        }

        HtBottomInset()
    }
}
