package com.imssolutions.rfid.ui

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.rfid.logic.PartChoice
import com.imssolutions.rfid.logic.partChoices

@Composable
fun StockPartsScreen(vm: AppViewModel) {
    val s = vm.strings
    val board = vm.board
    val choices = remember(board) { board?.let(::partChoices).orEmpty() }
    val chosen = vm.stockChoice
    val allChosen = choices.isNotEmpty() && choices.all { it.key in chosen }
    BackHandler { vm.backFromStockParts() }

    Column(Modifier.fillMaxSize()) {
        HtHeader(s.t("htChooseParts"), vm, onBack = vm::backFromStockParts)
        Row(
            Modifier
                .fillMaxWidth()
                .background(Color.White)
                .clickable(enabled = choices.isNotEmpty()) { vm.setAllStockParts(!allChosen) }
                .padding(horizontal = 6.dp, vertical = 4.dp),
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Checkbox(
                checked = allChosen,
                onCheckedChange = { vm.setAllStockParts(it) },
                enabled = choices.isNotEmpty(),
                colors = CheckboxDefaults.colors(checkedColor = Ht.Blue),
            )
            Text(s.t("htSelectAll"), color = Ht.Text, fontSize = 13.sp, fontWeight = FontWeight.Bold, modifier = Modifier.weight(1f))
            Text(
                s.t("htPartsChosen", chosen.size, choices.size),
                color = Ht.Muted,
                fontSize = 12.sp,
                modifier = Modifier.padding(end = 10.dp),
            )
        }
        HorizontalDivider(color = Ht.Line)
        LazyColumn(
            Modifier.weight(1f).fillMaxWidth(),
            contentPadding = PaddingValues(horizontal = 12.dp, vertical = 10.dp),
            verticalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            items(choices, key = { it.key }) { c -> PartRow(c, c.key in chosen) { vm.toggleStockPart(c.key) } }
        }
        Row(Modifier.fillMaxWidth().background(Color.White).padding(horizontal = 14.dp, vertical = 10.dp)) {
            ActionButton(s.t("htStartCount"), Ht.Blue, Modifier.weight(1f), enabled = board != null && chosen.isNotEmpty()) {
                vm.startStockCount()
            }
        }
    }
}

@Composable
private fun PartRow(c: PartChoice, checked: Boolean, onToggle: () -> Unit) {
    Row(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(10.dp))
            .background(if (checked) Color(0xFFEBF4FB) else Color.White)
            .clickable(onClick = onToggle)
            .padding(end = 12.dp, top = 4.dp, bottom = 4.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Checkbox(checked = checked, onCheckedChange = { onToggle() }, colors = CheckboxDefaults.colors(checkedColor = Ht.Blue))
        Column(Modifier.weight(1f)) {
            Text(
                c.itemName ?: c.itemCode ?: "—",
                color = Ht.TextStrong,
                fontSize = 14.sp,
                fontWeight = FontWeight.Bold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
            Text(c.itemCode ?: "—", color = Ht.Muted, fontSize = 12.sp, maxLines = 1, overflow = TextOverflow.Ellipsis)
        }
        Text("${c.scanned} / ${c.plan}", color = Ht.Blue, fontSize = 15.sp, fontWeight = FontWeight.ExtraBold)
    }
}
