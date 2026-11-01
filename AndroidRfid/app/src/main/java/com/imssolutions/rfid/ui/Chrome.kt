package com.imssolutions.rfid.ui

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.awaitEachGesture
import androidx.compose.foundation.gestures.awaitFirstDown
import androidx.compose.foundation.gestures.waitForUpOrCancellation
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.State
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.platform.LocalView
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.core.view.ViewCompat
import androidx.core.view.WindowInsetsCompat
import android.view.ViewTreeObserver
import com.imssolutions.rfid.R
import com.imssolutions.rfid.rfid.ReadMode
import com.imssolutions.rfid.rfid.ReaderState
import com.imssolutions.rfid.rfid.ReaderKind

object Ht {
    val Blue = Color(0xFF007AC9)
    val BlueDark = Color(0xFF005FA3)
    val Bg = Color(0xFFF5F6FA)
    val Text = Color(0xFF2D3748)
    val TextStrong = Color(0xFF1A202C)
    val Muted = Color(0xFF718096)
    val Label = Color(0xFF4A5568)
    val Line = Color(0xFFE2E8F0)
    val LineSoft = Color(0xFFF0F0F0)
    val Green = Color(0xFF38B85A)
    val Orange = Color(0xFFF29B2E)
    val OrangeText = Color(0xFFDD6B20)
    val Red = Color(0xFFE05A5A)
    val Logout = Color(0xFFE53E3E)
    val Gray = Color(0xFF718096)
    val BarYellow = Color(0xFFFFE033)
    val BarOrange = Color(0xFFFF8A3D)
    val BarGray = Color(0xFFDFE3E8)
    val BarLost = Color(0xFFFFCBA8)
    val Purple = Color(0xFF6B46C1)
    val BarTrack = Color(0xFFF0F2F5)
    val Trigger = Color(0xFFDD6B20)
    val TriggerOn = Color(0xFF9C4221)
    val MatchBg = Color(0xFFC6F6D5)
    val MatchText = Color(0xFF22543D)
    val ShortBg = Color(0xFFFED7D7)
    val ShortText = Color(0xFF9B2C2C)
}

private val FLAGS = listOf(
    "en" to R.drawable.flag_en,
    "ja" to R.drawable.flag_ja,
    "vi" to R.drawable.flag_vi,
)

@Composable
fun Flags(current: String, onPick: (String) -> Unit, big: Boolean = false, onBlue: Boolean = true) {
    val w = if (big) 28.dp else 20.dp
    val h = if (big) 19.dp else 14.dp
    Row(horizontalArrangement = Arrangement.spacedBy(if (big) 6.dp else 3.dp)) {
        FLAGS.forEach { (lang, res) ->
            val active = lang == current
            val edge = when {
                active && onBlue -> Color.White
                active -> Ht.Blue
                onBlue -> Color.White.copy(alpha = .3f)
                else -> Color(0xFFCBD5E0)
            }
            Image(
                painter = painterResource(res),
                contentDescription = lang,
                contentScale = ContentScale.Crop,
                modifier = Modifier
                    .size(w, h)
                    .clip(RoundedCornerShape(2.dp))
                    .border(1.dp, edge, RoundedCornerShape(2.dp))
                    .alpha(if (active) 1f else if (big) .4f else .5f)
                    .clickable { onPick(lang) },
            )
        }
    }
}

@Composable
fun HtHeader(title: String, vm: AppViewModel, onBack: (() -> Unit)? = null) {
    Row(
        modifier = Modifier
            .fillMaxWidth()
            .shadow(4.dp)
            .background(Ht.Blue)
            .height(46.dp)
            .padding(horizontal = 10.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(6.dp),
    ) {
        if (onBack != null) {
            Text(
                "←",
                color = Color.White,
                fontSize = 20.sp,
                fontWeight = FontWeight.Bold,
                modifier = Modifier
                    .clip(RoundedCornerShape(6.dp))
                    .clickable(onClick = onBack)
                    .padding(horizontal = 9.dp, vertical = 2.dp),
            )
        }
        Text(
            title,
            color = Color.White,
            fontSize = 15.sp,
            fontWeight = FontWeight.Bold,
            maxLines = 1,
            overflow = TextOverflow.Ellipsis,
            modifier = Modifier.weight(1f),
        )
        Flags(vm.strings.lang, vm::setLanguage)
    }
}

@Composable
fun Pill(color: Color, modifier: Modifier = Modifier, onClick: (() -> Unit)? = null, content: @Composable RowScope.() -> Unit) {
    Row(
        modifier = modifier
            .clip(RoundedCornerShape(14.dp))
            .background(color)
            .let { if (onClick != null) it.clickable(onClick = onClick) else it }
            .padding(horizontal = 12.dp, vertical = 5.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(5.dp),
        content = content,
    )
}

@Composable
fun PowerPill(vm: AppViewModel) {
    var open by remember { mutableStateOf(false) }
    val enabled = !vm.scanning && vm.applying == null && vm.rfidActive
    Box {
        Pill(if (enabled) Ht.Green else Ht.Green.copy(alpha = .45f), onClick = { if (enabled) open = true }) {
            Text(vm.strings.t("htPower"), color = Color.White, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
            Text("${vm.power} ▾", color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Bold)
        }
        DropdownMenu(expanded = open, onDismissRequest = { open = false }) {
            vm.powerRange.forEach { p ->
                DropdownMenuItem(
                    text = { Text("$p dBm", fontWeight = if (p == vm.power) FontWeight.Bold else FontWeight.Normal) },
                    onClick = {
                        open = false
                        vm.changePower(p)
                    },
                )
            }
        }
    }
}

@Composable
fun ModePill(vm: AppViewModel) {
    var open by remember { mutableStateOf(false) }
    val enabled = !vm.scanning && vm.applying == null
    val color = when (vm.mode) {
        ReadMode.Rfid -> Ht.Orange
        ReadMode.Barcode -> Ht.Blue
        ReadMode.Both -> Ht.Purple
    }
    val pick = vm.caps.modes.size > 2
    Box {
        Pill(if (enabled) color else color.copy(alpha = .45f), onClick = {
            if (!enabled) return@Pill
            if (pick) open = true else vm.toggleMode()
        }) {
            Text(vm.strings.t("htMode"), color = Color.White, fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
            Text(vm.modeLabel(vm.mode) + if (pick) " ▾" else " ⇄", color = Color.White, fontSize = 13.sp, fontWeight = FontWeight.Bold)
        }
        if (pick) {
            DropdownMenu(expanded = open, onDismissRequest = { open = false }) {
                vm.caps.modes.forEach { m ->
                    DropdownMenuItem(
                        text = { Text(vm.modeLabel(m), fontWeight = if (m == vm.mode) FontWeight.Bold else FontWeight.Normal) },
                        onClick = {
                            open = false
                            vm.changeMode(m)
                        },
                    )
                }
            }
        }
    }
}

@Composable
fun TriggerHint(vm: AppViewModel, modifier: Modifier = Modifier) {
    Text(
        vm.strings.t("htPressTrigger", vm.readerKind.label),
        color = Color.White,
        fontSize = 12.sp,
        fontWeight = FontWeight.SemiBold,
        modifier = modifier
            .clip(RoundedCornerShape(8.dp))
            .background(Ht.Blue)
            .padding(horizontal = 12.dp, vertical = 8.dp),
    )
}

@Composable
fun ActionButton(text: String, color: Color, modifier: Modifier = Modifier, enabled: Boolean = true, onClick: () -> Unit) {
    Box(
        modifier = modifier
            .clip(RoundedCornerShape(8.dp))
            .background(if (enabled) color else color.copy(alpha = .5f))
            .clickable(enabled = enabled, onClick = onClick)
            .padding(vertical = 12.dp),
        contentAlignment = Alignment.Center,
    ) {
        Text(text, color = Color.White, fontWeight = FontWeight.Bold, fontSize = 13.sp)
    }
}

@Composable
fun ReaderBanner(vm: AppViewModel) {
    val key = when (vm.readerState) {
        ReaderState.Ready -> return
        ReaderState.Unavailable -> "rdUnavailable"
        ReaderState.Connecting -> "rdConnecting"
        ReaderState.Waiting -> "rdWaiting"
        ReaderState.Detached -> "rdDetached"
    }
    Text(
        vm.strings.t(key),
        color = Color.White,
        fontSize = 11.sp,
        fontWeight = FontWeight.SemiBold,
        modifier = Modifier
            .fillMaxWidth()
            .background(
                when (vm.readerState) {
                    ReaderState.Connecting, ReaderState.Waiting -> Ht.Muted
                    else -> Ht.Logout
                },
            )
            .padding(horizontal = 14.dp, vertical = 4.dp),
    )
}

@Composable
fun rememberKeyboardOpen(): State<Boolean> {
    val view = LocalView.current
    val open = remember { mutableStateOf(false) }
    DisposableEffect(view) {
        val check = ViewTreeObserver.OnGlobalLayoutListener {
            open.value = ViewCompat.getRootWindowInsets(view)?.isVisible(WindowInsetsCompat.Type.ime()) == true
        }
        view.viewTreeObserver.addOnGlobalLayoutListener(check)
        check.onGlobalLayout()
        onDispose { view.viewTreeObserver.removeOnGlobalLayoutListener(check) }
    }
    return open
}

@Composable
fun ScanFab(vm: AppViewModel, modifier: Modifier = Modifier) {
    Box(
        modifier = modifier
            .size(if (vm.scanning) 68.dp else 60.dp)
            .shadow(6.dp, CircleShape)
            .clip(CircleShape)
            .background(if (vm.scanning) Ht.TriggerOn else Ht.Trigger)
            .pointerInput(Unit) {
                awaitEachGesture {
                    awaitFirstDown()
                    vm.startTrigger()
                    waitForUpOrCancellation()
                    vm.stopTrigger()
                }
            },
        contentAlignment = Alignment.Center,
    ) {
        Text(vm.strings.t("htScan"), color = Color.White, fontSize = 11.sp, fontWeight = FontWeight.ExtraBold)
    }
}

@Composable
fun HtDialogHost(vm: AppViewModel) {
    val d = vm.dialog ?: return
    AlertDialog(
        onDismissRequest = { if (d.confirm) vm.dialogCancel() else vm.dialogOk() },
        title = { Text(vm.strings.t("dlgInfoTitle"), fontSize = 16.sp, fontWeight = FontWeight.SemiBold) },
        text = { Text(d.message, fontSize = 13.sp, color = Color(0xFF424242)) },
        confirmButton = {
            TextButton(onClick = vm::dialogOk) {
                Text(vm.strings.t("dlgOk"), color = Ht.Blue, fontWeight = FontWeight.SemiBold)
            }
        },
        dismissButton = if (d.confirm) {
            {
                TextButton(onClick = vm::dialogCancel) {
                    Text(vm.strings.t("dlgCancel"), color = Ht.Blue, fontWeight = FontWeight.SemiBold)
                }
            }
        } else null,
        containerColor = Color.White,
        shape = RoundedCornerShape(6.dp),
    )
}

@Composable
fun DevicePicker(vm: AppViewModel, modifier: Modifier = Modifier) {
    Column(modifier.fillMaxWidth()) {
        Text(vm.strings.t("htDevice"), fontSize = 11.sp, color = Ht.Muted, modifier = Modifier.padding(bottom = 4.dp))
        Row(
            Modifier
                .fillMaxWidth()
                .height(IntrinsicSize.Min)
                .clip(RoundedCornerShape(8.dp))
                .border(1.5.dp, Color(0xFFCBD5E0), RoundedCornerShape(8.dp)),
        ) {
            ReaderKind.entries.forEachIndexed { i, kind ->
                if (i > 0) Box(Modifier.width(1.dp).fillMaxHeight().background(Color(0xFFCBD5E0)))
                val active = kind == vm.readerKind
                val split = kind.label.lastIndexOf(' ')
                val brand = if (split > 0) kind.label.substring(0, split) else kind.label
                val model = if (split > 0) kind.label.substring(split + 1) else ""
                Column(
                    modifier = Modifier
                        .weight(1f)
                        .fillMaxHeight()
                        .background(if (active) Ht.Blue else Color.White)
                        .clickable { vm.selectReader(kind) }
                        .padding(horizontal = 4.dp, vertical = 9.dp),
                    horizontalAlignment = Alignment.CenterHorizontally,
                    verticalArrangement = Arrangement.Center,
                ) {
                    Text(
                        brand,
                        color = if (active) Color.White else Ht.Text,
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Bold,
                        maxLines = 1,
                        overflow = TextOverflow.Ellipsis,
                        textAlign = TextAlign.Center,
                    )
                    if (model.isNotEmpty()) {
                        Text(
                            model,
                            color = if (active) Color.White.copy(alpha = .85f) else Ht.Muted,
                            fontSize = 11.sp,
                            fontWeight = FontWeight.SemiBold,
                            maxLines = 1,
                            textAlign = TextAlign.Center,
                        )
                    }
                }
            }
        }
    }
}
