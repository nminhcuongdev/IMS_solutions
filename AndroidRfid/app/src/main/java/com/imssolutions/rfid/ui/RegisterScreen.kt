package com.imssolutions.rfid.ui

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.PaddingValues
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.remember
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.rfid.logic.EPC_PREFIX_LENGTH
import com.imssolutions.rfid.logic.EpcGroup
import com.imssolutions.rfid.logic.PartInput
import com.imssolutions.rfid.logic.groupEpcs

private val REGISTER_FAB_GAP = 84.dp

@Composable
fun RegisterScreen(vm: AppViewModel) {
    val s = vm.strings
    val groups = remember(vm.regReads, vm.regKnown) { groupEpcs(vm.regReads, vm.regKnown) }
    val newCount = groups.sumOf { it.newEpcs.size }
    BackHandler { vm.backFromRegister() }

    Box(Modifier.fillMaxSize()) {
        Column(Modifier.fillMaxSize()) {
            HtHeader(s.t("htRegister"), vm, onBack = vm::backFromRegister)
            ReaderBanner(vm)
            Row(
                Modifier.fillMaxWidth().background(Color.White).padding(start = 14.dp, end = 14.dp, top = 12.dp, bottom = 4.dp),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.spacedBy(8.dp),
            ) {
                PowerPill(vm)
                Spacer(Modifier.weight(1f))
                Column(horizontalAlignment = Alignment.End) {
                    Text(s.t("htReadTags"), color = Ht.Muted, fontSize = 10.sp, fontWeight = FontWeight.Bold)
                    Text("${vm.regReads.size}", color = Ht.TextStrong, fontSize = 28.sp, fontWeight = FontWeight.ExtraBold)
                }
            }
            Text(
                s.t("htRegisterHint", EPC_PREFIX_LENGTH),
                color = Ht.Muted,
                fontSize = 11.sp,
                modifier = Modifier.fillMaxWidth().background(Color.White).padding(horizontal = 14.dp, vertical = 6.dp),
            )
            if (vm.regExcluded.isNotEmpty()) {
                Row(
                    Modifier.fillMaxWidth().background(Ht.ShortBg).padding(horizontal = 14.dp, vertical = 8.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(
                        s.t("htExcludedGroups", vm.regExcluded.size),
                        color = Ht.ShortText,
                        fontSize = 12.sp,
                        modifier = Modifier.weight(1f),
                    )
                    Text(
                        s.t("htRestoreGroups"),
                        color = Ht.Blue,
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Bold,
                        modifier = Modifier
                            .clip(RoundedCornerShape(8.dp))
                            .clickable(onClick = vm::restoreExcluded)
                            .padding(horizontal = 10.dp, vertical = 6.dp),
                    )
                }
            }
            HorizontalDivider(color = Ht.Line)
            LazyColumn(
                Modifier.weight(1f).fillMaxWidth(),
                contentPadding = PaddingValues(start = 12.dp, end = 12.dp, top = 10.dp, bottom = REGISTER_FAB_GAP),
                verticalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                items(groups, key = { it.prefix }) { g ->
                    GroupCard(vm, g, vm.regInputs[g.prefix] ?: PartInput())
                }
            }
            Row(
                Modifier.fillMaxWidth().background(Color.White).padding(horizontal = 14.dp, vertical = 10.dp),
                horizontalArrangement = Arrangement.spacedBy(10.dp),
            ) {
                ActionButton(s.t("htClear"), Ht.Gray, Modifier.weight(1f), enabled = vm.regReads.isNotEmpty() || vm.regExcluded.isNotEmpty()) { vm.clearRegister() }
                ActionButton(s.t("htRegisterBtn"), Ht.Red, Modifier.weight(1f), enabled = newCount > 0) { vm.saveRegister() }
            }
        }
        if (vm.softTriggerAvailable) {
            ScanFab(vm, Modifier.align(Alignment.BottomEnd).padding(end = 18.dp, bottom = REGISTER_FAB_GAP))
        } else {
            TriggerHint(vm, Modifier.align(Alignment.BottomCenter).padding(start = 16.dp, end = 16.dp, bottom = REGISTER_FAB_GAP))
        }
    }
}

@Composable
private fun GroupCard(vm: AppViewModel, g: EpcGroup, input: PartInput) {
    val s = vm.strings
    val done = g.newEpcs.isEmpty()
    Column(
        Modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(12.dp))
            .background(if (done) Ht.MatchBg else Color.White)
            .padding(12.dp),
        verticalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(
                g.prefix + "…",
                color = Ht.TextStrong,
                fontFamily = FontFamily.Monospace,
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
                modifier = Modifier.weight(1f),
            )
            Text("${g.total}", color = Ht.Blue, fontSize = 18.sp, fontWeight = FontWeight.ExtraBold)
            Text(
                "✕ " + s.t("htRemoveGroup"),
                color = Ht.Red,
                fontSize = 12.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier
                    .padding(start = 10.dp)
                    .clip(RoundedCornerShape(8.dp))
                    .border(1.5.dp, Ht.Red, RoundedCornerShape(8.dp))
                    .clickable { vm.excludeGroup(g.prefix) }
                    .padding(horizontal = 10.dp, vertical = 6.dp),
            )
        }
        Row(horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            if (g.newEpcs.isNotEmpty()) {
                Text(s.t("htGroupNew", g.newEpcs.size), color = Ht.OrangeText, fontSize = 11.sp, fontWeight = FontWeight.Bold)
            }
            if (g.registered.isNotEmpty()) {
                Text(s.t("htGroupRegistered", g.registered.size), color = Ht.MatchText, fontSize = 11.sp, fontWeight = FontWeight.Bold)
            }
        }
        if (done) {
            val owners = g.registered.values.map { listOfNotNull(it.code, it.name).joinToString(" ") }.distinct()
            Text(s.t("htGroupAllRegistered", owners.joinToString(", ")), color = Ht.MatchText, fontSize = 12.sp)
        } else {
            PartField(s.t("htPartNo"), input.code) { vm.setPartInput(g.prefix, input.copy(code = it)) }
            PartField(s.t("htPartName"), input.name) { vm.setPartInput(g.prefix, input.copy(name = it)) }
        }
    }
}

@Composable
private fun PartField(label: String, value: String, onChange: (String) -> Unit) {
    OutlinedTextField(
        value = value,
        onValueChange = onChange,
        singleLine = true,
        label = { Text(label, fontSize = 11.sp) },
        shape = RoundedCornerShape(8.dp),
        colors = OutlinedTextFieldDefaults.colors(
            focusedBorderColor = Ht.Blue,
            unfocusedBorderColor = Color(0xFFCBD5E0),
            focusedContainerColor = Color(0xFFF7FAFC),
            unfocusedContainerColor = Color(0xFFF7FAFC),
        ),
        modifier = Modifier.fillMaxWidth(),
    )
}
