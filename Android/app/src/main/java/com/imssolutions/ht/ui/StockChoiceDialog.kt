package com.imssolutions.ht.ui

import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.data.StockLine
import com.imssolutions.ht.ui.vm.fmt

@Composable
fun StockChoiceDialog(choices: List<StockLine>, onChoose: (String) -> Unit, onDismiss: () -> Unit) {
    if (choices.isEmpty()) return
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(t("htChooseStockRow", "Select the stock row")) },
        text = {
            LazyColumn {
                items(choices, key = { it.itemCode + "/" + it.serialNo }) { c ->
                    Column(
                        Modifier.fillMaxWidth().clickable { onChoose(c.serialNo) }.padding(vertical = 8.dp),
                    ) {
                        Text(c.serialNo, fontWeight = FontWeight.SemiBold, fontSize = 14.sp)
                        Text(
                            t("htStockRowInfo", "{0} · Lot {1} · Qty {2}", c.locationCode ?: "—", c.lotNo ?: "—", fmt(c.stockQty)),
                            fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurfaceVariant,
                        )
                    }
                    HorizontalDivider()
                }
            }
        },
        confirmButton = {},
        dismissButton = { TextButton(onClick = onDismiss) { Text(t("htCancel", "Cancel")) } },
    )
}
