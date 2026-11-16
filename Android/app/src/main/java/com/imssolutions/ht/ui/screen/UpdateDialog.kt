package com.imssolutions.ht.ui.screen

import android.widget.Toast
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.LinearProgressIndicator
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.compose.ui.window.DialogProperties
import com.imssolutions.ht.ui.LocalHtStrings
import com.imssolutions.ht.ui.render
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.vm.UpdatePhase
import com.imssolutions.ht.ui.vm.UpdateViewModel
import com.imssolutions.ht.update.ApkInstaller
import com.imssolutions.ht.update.UpdateRules

@Composable
fun UpdateHost(vm: UpdateViewModel) {
    val state by vm.state.collectAsState()
    val context = LocalContext.current
    val strings = LocalHtStrings.current

    LaunchedEffect(state.notice) {
        state.notice?.let {
            Toast.makeText(context, strings.render(it), Toast.LENGTH_LONG).show()
            vm.clearNotice()
        }
    }

    val offer = state.offer ?: return
    if (!state.dialog) return
    val e = offer.entry
    val busy = state.phase == UpdatePhase.DOWNLOADING || state.phase == UpdatePhase.VERIFYING
    val canLater = !offer.forced && !busy

    AlertDialog(
        onDismissRequest = { if (canLater) vm.later() },
        properties = DialogProperties(dismissOnBackPress = canLater, dismissOnClickOutside = false),
        title = {
            Text(
                if (offer.forced) t("htUpdTitleForced", "An app update is required") else t("htUpdTitle", "A new version is available"),
                fontWeight = FontWeight.Bold,
            )
        },
        text = {
            Column(Modifier.verticalScroll(rememberScrollState())) {
                Text(t("htUpdVersion", "Version: {0} → {1}", vm.installedName, e.versionName), fontSize = 14.sp)
                e.updatedAt?.let { Text(t("htUpdDate", "Released: {0}", it.take(16).replace('T', ' ')), fontSize = 12.sp) }
                UpdateRules.note(e, strings.lang.code)?.let {
                    Spacer(Modifier.height(8.dp))
                    Text(t("htUpdNotes", "What is new"), fontSize = 12.sp, fontWeight = FontWeight.SemiBold)
                    Text(it, fontSize = 13.sp)
                }
                if (offer.forced && state.phase == UpdatePhase.OFFER) {
                    Spacer(Modifier.height(8.dp))
                    Text(t("htUpdForcedMsg", "You must update to keep using the app."), fontSize = 12.sp, color = Color(0xFFDC3545))
                }
                when (state.phase) {
                    UpdatePhase.DOWNLOADING -> {
                        Spacer(Modifier.height(12.dp))
                        if (state.total > 0) {
                            LinearProgressIndicator(progress = { state.done.toFloat() / state.total }, modifier = Modifier.fillMaxWidth())
                        } else {
                            LinearProgressIndicator(Modifier.fillMaxWidth())
                        }
                        Spacer(Modifier.height(4.dp))
                        Text(
                            t("htUpdProgress", "Downloading: {0} / {1} MB ({2}%)", mb(state.done), mb(state.total),
                                if (state.total > 0) state.done * 100 / state.total else 0),
                            fontSize = 12.sp,
                        )
                    }
                    UpdatePhase.VERIFYING -> {
                        Spacer(Modifier.height(12.dp))
                        LinearProgressIndicator(Modifier.fillMaxWidth())
                        Text(t("htUpdVerifying", "Checking the file..."), fontSize = 12.sp)
                    }
                    UpdatePhase.INSTALLING -> {
                        Spacer(Modifier.height(12.dp))
                        Text(t("htUpdInstalling", "Opening the installer. Choose \"Install\"."), fontSize = 12.sp)
                    }
                    UpdatePhase.PERMISSION -> {
                        Spacer(Modifier.height(12.dp))
                        Text(
                            t("htUpdPermission", "Allow this app to install apps (Install unknown apps), then come back and choose Continue."),
                            fontSize = 12.sp, color = MaterialTheme.colorScheme.error,
                        )
                    }
                    UpdatePhase.FAILED -> state.error?.let {
                        Spacer(Modifier.height(12.dp))
                        Text(strings.render(it), fontSize = 12.sp, color = MaterialTheme.colorScheme.error)
                    }
                    UpdatePhase.OFFER -> Unit
                }
            }
        },
        confirmButton = {
            when (state.phase) {
                UpdatePhase.OFFER -> Button(onClick = vm::update) { Text(t("htUpdNow", "Update")) }
                UpdatePhase.FAILED, UpdatePhase.INSTALLING -> Button(onClick = vm::update) { Text(t("htUpdRetry", "Retry")) }
                UpdatePhase.PERMISSION -> Column {
                    TextButton(onClick = { context.startActivity(ApkInstaller.permissionIntent(context)) }) {
                        Text(t("htUpdOpenSettings", "Open settings"))
                    }
                    Button(onClick = vm::update) { Text(t("htUpdContinue", "Continue")) }
                }
                else -> Unit
            }
        },
        dismissButton = if (canLater) {
            { TextButton(onClick = vm::later) { Text(t("htUpdLater", "Later")) } }
        } else null,
    )
}

private fun mb(bytes: Long): String = String.format(java.util.Locale.US, "%.1f", bytes / 1048576.0)
