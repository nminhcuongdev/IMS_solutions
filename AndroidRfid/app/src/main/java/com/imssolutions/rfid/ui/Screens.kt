package com.imssolutions.rfid.ui

import androidx.activity.compose.BackHandler
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CheckboxDefaults
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.OutlinedTextFieldDefaults
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.produceState
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.KeyboardType
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import kotlinx.coroutines.delay
import com.imssolutions.rfid.data.ServerAddress
import com.imssolutions.rfid.logic.BarKind
import com.imssolutions.rfid.logic.LineStatus
import com.imssolutions.rfid.logic.StockLine
import com.imssolutions.rfid.logic.partChoices

@Composable
fun HtApp(vm: AppViewModel) {
    Box(Modifier.fillMaxSize().background(Ht.Bg)) {
        when (vm.screen) {
            Screen.Login -> LoginScreen(vm)
            Screen.Menu -> MenuScreen(vm)
            Screen.StockParts -> StockPartsScreen(vm)
            Screen.StockCount -> StockCountScreen(vm)
            Screen.Search -> SearchScreen(vm)
            Screen.SearchDetail -> SearchDetailScreen(vm)
            Screen.Register -> RegisterScreen(vm)
            Screen.Settings -> SettingsScreen(vm)
        }
        if (vm.busy) {
            Box(
                Modifier.fillMaxSize().background(Color.Black.copy(alpha = .15f)).clickable(enabled = true) {},
                contentAlignment = Alignment.Center,
            ) { CircularProgressIndicator(color = Ht.Blue) }
        }
        vm.applying?.let { message ->
            Box(
                Modifier.fillMaxSize().background(Color.Black.copy(alpha = .35f)).clickable(enabled = true) {},
                contentAlignment = Alignment.Center,
            ) {
                Column(
                    Modifier.clip(RoundedCornerShape(12.dp)).background(Color.White).padding(horizontal = 28.dp, vertical = 22.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.spacedBy(14.dp),
                ) {
                    CircularProgressIndicator(color = Ht.Blue)
                    Text(message, color = Ht.TextStrong, fontSize = 15.sp, fontWeight = FontWeight.SemiBold)
                }
            }
        }
        HtDialogHost(vm)
    }
}

@Composable
private fun fieldColors() = OutlinedTextFieldDefaults.colors(
    focusedBorderColor = Ht.Blue,
    unfocusedBorderColor = Color(0xFFCBD5E0),
    focusedContainerColor = Color(0xFFF7FAFC),
    unfocusedContainerColor = Color(0xFFF7FAFC),
)

@Composable
private fun LoginScreen(vm: AppViewModel) {
    val s = vm.strings
    var userId by remember { mutableStateOf(vm.prefs.lastUserId) }
    var password by remember { mutableStateOf("") }
    var rememberMe by remember { mutableStateOf(vm.prefs.rememberMe) }
    val submit = { vm.login(userId, password, rememberMe) }

    Box(Modifier.fillMaxSize().background(Color.White)) {
    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(Color.White)
            .verticalScroll(rememberScrollState())
            .padding(28.dp),
        horizontalAlignment = Alignment.CenterHorizontally,
        verticalArrangement = Arrangement.Center,
    ) {
        Text("IMS", color = Ht.Blue, fontSize = 26.sp, fontWeight = FontWeight.ExtraBold)
        Text("RFID", color = Ht.Muted, fontSize = 11.sp, fontWeight = FontWeight.SemiBold)
        Spacer(Modifier.height(32.dp))
        LoginField(s.t("htUserId"), userId, { userId = it })
        LoginField(s.t("htPassword"), password, { password = it }, password = true, onDone = submit)
        Row(
            Modifier.fillMaxWidth().clickable { rememberMe = !rememberMe },
            verticalAlignment = Alignment.CenterVertically,
        ) {
            Checkbox(
                checked = rememberMe,
                onCheckedChange = { rememberMe = it },
                colors = CheckboxDefaults.colors(checkedColor = Ht.Blue),
            )
            Text(s.t("htRememberMe"), color = Ht.Text, fontSize = 13.sp)
        }
        Box(
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 10.dp)
                .clip(RoundedCornerShape(8.dp))
                .background(Ht.Blue)
                .clickable(onClick = submit)
                .padding(12.dp),
            contentAlignment = Alignment.Center,
        ) {
            Text(s.t("htLoginBtn"), color = Color.White, fontWeight = FontWeight.Bold, fontSize = 15.sp)
        }
        Spacer(Modifier.height(20.dp))
        Flags(s.lang, vm::setLanguage, big = true, onBlue = false)
    }
    Text(
        "⚙",
        color = Ht.Muted,
        fontSize = 26.sp,
        modifier = Modifier
            .align(Alignment.TopEnd)
            .padding(10.dp)
            .clip(RoundedCornerShape(8.dp))
            .clickable(onClick = vm::openSettings)
            .padding(horizontal = 10.dp, vertical = 4.dp),
    )
    }
}

@Composable
private fun LoginField(
    label: String,
    value: String,
    onChange: (String) -> Unit,
    password: Boolean = false,
    keyboard: KeyboardType = KeyboardType.Text,
    onDone: (() -> Unit)? = null,
) {
    Column(Modifier.fillMaxWidth().padding(bottom = 14.dp)) {
        Text(label, fontSize = 11.sp, color = Ht.Muted, modifier = Modifier.padding(bottom = 4.dp))
        OutlinedTextField(
            value = value,
            onValueChange = onChange,
            singleLine = true,
            visualTransformation = if (password) PasswordVisualTransformation() else androidx.compose.ui.text.input.VisualTransformation.None,
            keyboardOptions = KeyboardOptions(
                keyboardType = if (password) KeyboardType.Password else keyboard,
                imeAction = if (onDone != null) ImeAction.Done else ImeAction.Next,
            ),
            keyboardActions = KeyboardActions(onDone = { onDone?.invoke() }),
            shape = RoundedCornerShape(8.dp),
            colors = fieldColors(),
            modifier = Modifier.fillMaxWidth(),
        )
    }
}

@Composable
private fun MenuScreen(vm: AppViewModel) {
    val s = vm.strings
    BackHandler { vm.logout() }
    Column(Modifier.fillMaxSize()) {
        HtHeader(s.t("htMenu"), vm)
        ReaderBanner(vm)
        Column(
            Modifier.fillMaxSize().padding(14.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            MenuItem(s.t("htStockCount"), vm::openStockCount)
            MenuItem(s.t("htSearch"), vm::openSearch)
            MenuItem(s.t("htRegister"), vm::openRegister)
            MenuItem(s.t("htSettings"), vm::openSettings)
            Spacer(Modifier.weight(1f))
            Row(
                modifier = Modifier
                    .fillMaxWidth()
                    .shadow(2.dp, RoundedCornerShape(12.dp))
                    .background(Color.White, RoundedCornerShape(12.dp))
                    .padding(12.dp),
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Column(Modifier.weight(1f)) {
                    Text(vm.userName.ifBlank { "-" }, color = Ht.Text, fontSize = 13.sp, fontWeight = FontWeight.Bold)
                    Text(vm.userCode, color = Ht.Label, fontSize = 12.sp)
                }
                Text(
                    s.t("htLogout"),
                    color = Ht.Logout,
                    fontSize = 11.sp,
                    fontWeight = FontWeight.SemiBold,
                    modifier = Modifier
                        .clip(RoundedCornerShape(8.dp))
                        .border(1.5.dp, Ht.Logout, RoundedCornerShape(8.dp))
                        .clickable(onClick = vm::logout)
                        .padding(horizontal = 12.dp, vertical = 6.dp),
                )
            }
        }
    }
}

@Composable
private fun MenuItem(title: String, onClick: () -> Unit) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(3.dp, RoundedCornerShape(12.dp))
            .clip(RoundedCornerShape(12.dp))
            .background(Color.White)
            .clickable(onClick = onClick),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Box(Modifier.width(4.dp).height(56.dp).background(Ht.Blue))
        Text(
            title,
            color = Ht.Text,
            fontSize = 14.sp,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.weight(1f).padding(horizontal = 14.dp),
        )
        Text("›", color = Color(0xFFA0AEC0), fontSize = 20.sp, modifier = Modifier.padding(end = 16.dp))
    }
}

@Composable
private fun StockCountScreen(vm: AppViewModel) {
    val s = vm.strings
    val board = vm.countBoard
    val lines = remember(board, vm.pending) { board?.stockLines(vm.pending).orEmpty() }
    val total = lines.sumOf { it.qty }
    BackHandler { vm.backFromStockCount() }

    Box(Modifier.fillMaxSize()) {
    Column(Modifier.fillMaxSize()) {
        HtHeader(s.t("htStockCount"), vm, onBack = vm::backFromStockCount)
        ReaderBanner(vm)
        Row(
            Modifier.fillMaxWidth().background(Color.White).padding(start = 14.dp, end = 14.dp, top = 12.dp, bottom = 6.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            PowerPill(vm)
            if (vm.caps.modes.size > 1) ModePill(vm)
            Pill(Ht.Blue, onClick = vm::backFromStockCount) {
                Text(s.t("htPartsPill", vm.stockChoice.size, vm.board?.let { partChoices(it).size } ?: 0), color = Color.White, fontSize = 12.sp, fontWeight = FontWeight.Bold)
            }
            Spacer(Modifier.weight(1f))
            if (vm.pending.isNotEmpty()) {
                Text(s.t("htPending", vm.pending.size), color = Ht.OrangeText, fontSize = 11.sp, fontWeight = FontWeight.Bold)
            }
        }
        Column(
            Modifier.fillMaxWidth().background(Color.White).padding(start = 14.dp, end = 14.dp, top = 2.dp, bottom = 12.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
        ) {
            Text(s.t("htTotalCount"), color = Ht.Text, fontSize = 18.sp, fontWeight = FontWeight.ExtraBold)
            Text("$total", color = Ht.TextStrong, fontSize = 80.sp, fontWeight = FontWeight.ExtraBold, lineHeight = 84.sp)
        }
        HorizontalDivider(color = Ht.Line)
        Column(Modifier.weight(1f).fillMaxWidth().background(Color.White).padding(horizontal = 14.dp)) {
            StockRow(s.t("htProduct"), s.t("htQuantity"), s.t("htPlan"), header = true)
            HorizontalDivider(thickness = 2.dp, color = Ht.Line)
            LazyColumn(Modifier.fillMaxSize()) {
                items(lines, key = { "${it.itemCode}|${it.itemName}" }) { line -> StockLineRow(line) }
            }
        }
        Row(
            Modifier.fillMaxWidth().background(Color.White).padding(horizontal = 14.dp, vertical = 10.dp),
        ) {
            ActionButton(s.t("htSave"), Ht.Red, Modifier.weight(1f), enabled = board != null) { vm.saveStockCount() }
        }
    }
    if (vm.softTriggerAvailable) {
        ScanFab(vm, Modifier.align(Alignment.BottomEnd).padding(end = 18.dp, bottom = FAB_ABOVE_ACTIONS))
    } else {
        TriggerHint(vm, Modifier.align(Alignment.BottomCenter).padding(start = 16.dp, end = 16.dp, bottom = FAB_ABOVE_ACTIONS))
    }
    }
}

@Composable
private fun StockLineRow(line: StockLine) {
    StockRow(line.itemName, "${line.qty}", "${line.plan}", status = line.status)
    HorizontalDivider(color = Ht.LineSoft)
}

@Composable
private fun StockRow(product: String, qty: String, plan: String, header: Boolean = false, status: LineStatus = LineStatus.NotScanned) {
    val color = when {
        header -> Ht.Blue
        status == LineStatus.Match -> Ht.MatchText
        status == LineStatus.Short -> Ht.ShortText
        else -> Ht.TextStrong
    }
    val background = when {
        header -> Color.Transparent
        status == LineStatus.Match -> Ht.MatchBg
        status == LineStatus.Short -> Ht.ShortBg
        else -> Color.White
    }
    val weight = if (header || status != LineStatus.NotScanned) FontWeight.Bold else FontWeight.Normal
    val size = if (header) 12.sp else 13.sp
    Row(Modifier.fillMaxWidth().background(background).padding(vertical = 8.dp, horizontal = 4.dp)) {
        Text(product, color = color, fontWeight = weight, fontSize = size, modifier = Modifier.weight(1f), maxLines = 2, overflow = TextOverflow.Ellipsis)
        Text(qty, color = color, fontWeight = weight, fontSize = size, textAlign = TextAlign.End, modifier = Modifier.width(72.dp))
        Text(plan, color = color, fontWeight = weight, fontSize = size, textAlign = TextAlign.End, modifier = Modifier.width(56.dp))
    }
}

@Composable
private fun SearchScreen(vm: AppViewModel) {
    val s = vm.strings
    val names = remember(vm.board) { vm.board?.itemNames().orEmpty() }
    var serial by remember { mutableStateOf("") }
    var open by remember { mutableStateOf(false) }
    val keyboardOpen by rememberKeyboardOpen()
    BackHandler { vm.backToMenu() }

    Box(Modifier.fillMaxSize()) {
    Column(Modifier.fillMaxSize()) {
        HtHeader(s.t("htSearch"), vm, onBack = vm::backToMenu)
        ReaderBanner(vm)
        Column(
            Modifier.weight(1f).fillMaxWidth().background(Color.White).verticalScroll(rememberScrollState())
                .padding(horizontal = 16.dp, vertical = 6.dp),
        ) {
            Row(Modifier.padding(top = 12.dp)) { PowerPill(vm) }

            SectionLabel(s.t("htProductNameLabel"))
            Box {
                Row(
                    modifier = Modifier
                        .fillMaxWidth()
                        .clip(RoundedCornerShape(8.dp))
                        .border(1.5.dp, if (open) Ht.Blue else Color(0xFFCBD5E0), RoundedCornerShape(8.dp))
                        .clickable { open = true }
                        .padding(12.dp),
                    verticalAlignment = Alignment.CenterVertically,
                ) {
                    Text(vm.searchItem ?: "-", color = Ht.TextStrong, fontSize = 16.sp, fontWeight = FontWeight.SemiBold, modifier = Modifier.weight(1f))
                    Text("▾", color = Ht.Muted)
                }
                DropdownMenu(expanded = open, onDismissRequest = { open = false }) {
                    names.forEach { n ->
                        DropdownMenuItem(text = { Text(n) }, onClick = {
                            open = false
                            vm.selectSearchItem(n)
                        })
                    }
                }
            }

            SectionLabel(s.t("htSerialNo"))
            OutlinedTextField(
                value = serial,
                onValueChange = { serial = it },
                singleLine = true,
                placeholder = { Text(s.t("htSerialPlaceholder"), fontSize = 14.sp) },
                keyboardOptions = KeyboardOptions(imeAction = ImeAction.Done),
                keyboardActions = KeyboardActions(onDone = { if (vm.addSerial(serial)) serial = "" }),
                shape = RoundedCornerShape(8.dp),
                colors = fieldColors(),
                modifier = Modifier.fillMaxWidth(),
            )

            Text(
                s.t("htReadQuantity"),
                color = Ht.Label,
                fontSize = 13.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier.padding(top = 18.dp),
            )
            Box(
                Modifier.fillMaxWidth().padding(top = 8.dp).height(90.dp)
                    .clip(RoundedCornerShape(10.dp)).background(Ht.BarTrack),
                contentAlignment = Alignment.Center,
            ) {
                Text("${vm.searchRead.size}", color = Ht.TextStrong, fontSize = 44.sp, fontWeight = FontWeight.ExtraBold)
            }
        }
        Row(
            Modifier.fillMaxWidth().background(Color.White).padding(horizontal = 14.dp, vertical = 10.dp),
            horizontalArrangement = Arrangement.spacedBy(8.dp),
        ) {
            ActionButton(s.t("htClear"), Ht.Gray, Modifier.weight(1f)) { vm.clearSearch() }
            ActionButton(s.t("htDetailSearch"), Ht.Red, Modifier.weight(1f)) { vm.openSearchDetail() }
        }
    }
    if (!keyboardOpen) {
        ScanFab(vm, Modifier.align(Alignment.BottomEnd).padding(end = 18.dp, bottom = FAB_ABOVE_ACTIONS))
    }
    }
}

@Composable
private fun SectionLabel(text: String) {
    Text(
        text,
        color = Ht.Label,
        fontSize = 13.sp,
        fontWeight = FontWeight.Bold,
        modifier = Modifier.padding(top = 18.dp, bottom = 8.dp),
    )
}

@Composable
private fun SearchDetailScreen(vm: AppViewModel) {
    val s = vm.strings
    val now by produceState(System.currentTimeMillis()) {
        while (true) {
            delay(SIGNAL_TICK_MS)
            value = System.currentTimeMillis()
        }
    }
    val bars = vm.board?.searchBars(vm.searchItem, vm.searchRead, now).orEmpty()
    BackHandler { vm.backFromSearchDetail() }

    Box(Modifier.fillMaxSize()) {
    Column(Modifier.fillMaxSize()) {
        HtHeader(s.t("htSearchDetailTitle"), vm, onBack = vm::backFromSearchDetail)
        ReaderBanner(vm)
        Row(
            Modifier.fillMaxWidth().background(Color.White).padding(horizontal = 16.dp, vertical = 12.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(10.dp),
        ) {
            PowerPill(vm)
            Text(
                vm.searchItem ?: "-",
                color = Ht.TextStrong,
                fontSize = 20.sp,
                fontWeight = FontWeight.ExtraBold,
                maxLines = 1,
                overflow = TextOverflow.Ellipsis,
            )
        }
        HorizontalDivider(color = Ht.Line)
        Text(
            s.t("htSerialNoHeader"),
            color = Ht.Label,
            fontSize = 12.sp,
            fontWeight = FontWeight.Bold,
            modifier = Modifier.fillMaxWidth().background(Color(0xFFF7FAFC)).padding(horizontal = 16.dp, vertical = 8.dp),
        )
        HorizontalDivider(color = Ht.Line)
        LazyColumn(Modifier.weight(1f).fillMaxWidth().background(Color.White).padding(horizontal = 16.dp, vertical = 4.dp)) {
            items(bars, key = { it.epc }) { bar ->
                Column(Modifier.fillMaxWidth().padding(vertical = 6.dp)) {
                    Row(Modifier.fillMaxWidth(), verticalAlignment = Alignment.CenterVertically) {
                        Text(
                            bar.epc,
                            color = if (bar.kind == BarKind.Lost) Ht.Muted else Ht.Text,
                            fontSize = 13.sp,
                            fontWeight = FontWeight.SemiBold,
                            fontFamily = FontFamily.Monospace,
                            softWrap = true,
                            modifier = Modifier.weight(1f),
                        )
                        Text(
                            if (bar.kind == BarKind.Read || bar.kind == BarKind.Lost) "${bar.percent}%" else "",
                            color = Ht.Muted,
                            fontSize = 12.sp,
                            fontWeight = FontWeight.Bold,
                            textAlign = TextAlign.End,
                            modifier = Modifier.width(44.dp),
                        )
                    }
                    Box(
                        Modifier.fillMaxWidth().padding(top = 4.dp).height(14.dp).clip(RoundedCornerShape(3.dp)).background(Ht.BarTrack),
                    ) {
                        Box(
                            Modifier.fillMaxHeight().fillMaxWidth(bar.percent / 100f).clip(RoundedCornerShape(3.dp)).background(
                                when (bar.kind) {
                                    BarKind.Confirmed -> Ht.BarYellow
                                    BarKind.Read -> Ht.BarOrange
                                    BarKind.Lost -> Ht.BarLost
                                    BarKind.Unread -> Ht.BarGray
                                },
                            ),
                        )
                    }
                }
            }
        }
    }
    ScanFab(vm, Modifier.align(Alignment.BottomEnd).padding(end = 18.dp, bottom = 18.dp))
    }
}

private val FAB_ABOVE_ACTIONS = 84.dp
private const val SIGNAL_TICK_MS = 250L

@Composable
private fun SettingsScreen(vm: AppViewModel) {
    val s = vm.strings
    var server by remember { mutableStateOf(ServerAddress.toDisplay(vm.prefs.serverUrl)) }
    val resolved = vm.resolveServer(server)
    val save = { vm.saveServer(server) { server = it } }
    BackHandler { vm.backFromSettings() }

    Column(Modifier.fillMaxSize()) {
        HtHeader(s.t("htSettings"), vm, onBack = vm::backFromSettings)
        ReaderBanner(vm)
        Column(
            Modifier.weight(1f).fillMaxWidth().background(Color.White).verticalScroll(rememberScrollState())
                .padding(horizontal = 16.dp, vertical = 16.dp),
        ) {
            LoginField(s.t("htServer"), server, { server = it }, keyboard = KeyboardType.Uri, onDone = { save() })
            Text(
                when {
                    server.isBlank() -> ""
                    resolved.isEmpty() -> s.t("errServerInvalid")
                    resolved.size == 1 -> resolved.first()
                    else -> s.t("htPortAuto", resolved.first().removePrefix("http://").substringBefore(':'), resolved.joinToString(", ") { it.trimEnd('/').substringAfterLast(':') })
                },
                color = if (resolved.isEmpty()) Ht.Logout else Ht.Muted,
                fontSize = 11.sp,
                modifier = Modifier.padding(start = 4.dp, bottom = 10.dp),
            )
            ActionButton(s.t("htSaveServer"), Ht.Blue, Modifier.fillMaxWidth()) { save() }
            if (vm.savedServers.isNotEmpty()) {
                Text(
                    s.t("htSavedServers"),
                    color = Ht.Muted,
                    fontSize = 11.sp,
                    fontWeight = FontWeight.Bold,
                    modifier = Modifier.padding(start = 4.dp, top = 18.dp, bottom = 6.dp),
                )
                val current = vm.prefs.serverUrl
                vm.savedServers.forEach { url ->
                    val active = ServerAddress.sameServer(url, current)
                    Row(
                        Modifier
                            .fillMaxWidth()
                            .padding(bottom = 6.dp)
                            .clip(RoundedCornerShape(8.dp))
                            .background(if (active) Color(0xFFEBF4FB) else Color(0xFFF7FAFC))
                            .border(1.dp, if (active) Ht.Blue else Color(0xFFCBD5E0), RoundedCornerShape(8.dp))
                            .clickable {
                                server = ServerAddress.toDisplay(url)
                                if (!active) save()
                            }
                            .padding(start = 12.dp, top = 4.dp, bottom = 4.dp),
                        verticalAlignment = Alignment.CenterVertically,
                    ) {
                        Text(
                            (if (active) "● " else "") + ServerAddress.toDisplay(url),
                            color = if (active) Ht.Blue else Ht.Text,
                            fontSize = 14.sp,
                            fontWeight = if (active) FontWeight.Bold else FontWeight.Normal,
                            modifier = Modifier.weight(1f).padding(vertical = 8.dp),
                        )
                        if (!active) {
                            Text(
                                "✕",
                                color = Ht.Muted,
                                fontSize = 16.sp,
                                modifier = Modifier
                                    .clickable { vm.forgetServer(url) }
                                    .padding(horizontal = 14.dp, vertical = 8.dp),
                            )
                        }
                    }
                }
            }
            Spacer(Modifier.height(22.dp))
            DevicePicker(vm)
        }
    }
}
