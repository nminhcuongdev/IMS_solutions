package com.imssolutions.ht.ui.screen

import com.imssolutions.ht.ui.BusyOverlay
import com.imssolutions.ht.ui.t
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.KeyboardActions
import androidx.compose.foundation.text.KeyboardOptions
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.Login
import androidx.compose.material.icons.filled.Settings
import androidx.compose.material.icons.filled.Visibility
import androidx.compose.material.icons.filled.VisibilityOff
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.input.ImeAction
import androidx.compose.ui.text.input.PasswordVisualTransformation
import androidx.compose.ui.text.input.VisualTransformation
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.viewmodel.compose.viewModel
import kotlinx.coroutines.launch
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.BuildConfig
import com.imssolutions.ht.R
import com.imssolutions.ht.ui.ErrorBar
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.HtStatusBar
import com.imssolutions.ht.ui.LangFlags
import com.imssolutions.ht.ui.nowClock
import com.imssolutions.ht.ui.theme.BrandBlue
import com.imssolutions.ht.ui.vm.LoginViewModel

@Composable
fun LoginScreen(
    lang: HtLang,
    onLang: (HtLang) -> Unit,
    onLoggedIn: () -> Unit,
    onOpenSettings: () -> Unit,
) {
    val context = LocalContext.current
    val vm: LoginViewModel = viewModel()
    val state by vm.state.collectAsState()
    BusyOverlay(state.loading)
    var showPassword by remember { mutableStateOf(false) }
    val baseUrl by ApiClient.tokens.baseUrl.collectAsState(initial = BuildConfig.API_BASE_URL)
    val langDataAt by ApiClient.tokens.langDataAt.collectAsState(initial = null)
    var serverApk by remember { mutableStateOf<String?>(null) }
    var apkUrl by remember { mutableStateOf<String?>(null) }
    LaunchedEffect(baseUrl) {
        runCatching { com.imssolutions.ht.data.ReceivingRepository().config() }
            .onSuccess { serverApk = it.apkVersion; apkUrl = it.apkDownloadUrl }
    }
    val hasUpdate = serverApk != null && !apkUrl.isNullOrBlank() &&
        isNewerVersion(serverApk!!, BuildConfig.VERSION_NAME)

    LaunchedEffect(state.success) { if (state.success) onLoggedIn() }

    Column(Modifier.fillMaxSize().background(Color.White).statusBarsPadding()) {
        Column(
            Modifier
                .weight(1f)
                .fillMaxWidth()
                .padding(28.dp),
            horizontalAlignment = Alignment.CenterHorizontally,
            verticalArrangement = Arrangement.Center,
        ) {
            Text(
                "IMS",
                fontSize = 34.sp,
                color = BrandBlue,
                fontWeight = FontWeight.ExtraBold,
                letterSpacing = 4.sp,
            )
            Spacer(Modifier.height(6.dp))
            Text(
                t("htAppSubtitle", "Inventory Management System"),
                fontSize = 11.sp,
                color = BrandBlue,
                fontWeight = FontWeight.SemiBold,
            )

            Spacer(Modifier.height(36.dp))

            HtField(label = t("userId", "User ID")) {
                OutlinedTextField(
                    value = state.userCode,
                    onValueChange = vm::setUserCode,
                    singleLine = true,
                    shape = RoundedCornerShape(8.dp),
                    keyboardOptions = KeyboardOptions(imeAction = ImeAction.Next),
                    modifier = Modifier.fillMaxWidth(),
                )
            }

            HtField(label = t("password", "Password")) {
                OutlinedTextField(
                    value = state.password,
                    onValueChange = vm::setPassword,
                    singleLine = true,
                    shape = RoundedCornerShape(8.dp),
                    trailingIcon = {
                        IconButton(onClick = { showPassword = !showPassword }) {
                            Icon(
                                if (showPassword) Icons.Default.VisibilityOff else Icons.Default.Visibility,
                                contentDescription = if (showPassword) "Ẩn mật khẩu" else "Hiện mật khẩu",
                            )
                        }
                    },
                    visualTransformation = if (showPassword) VisualTransformation.None
                                           else PasswordVisualTransformation(),
                    keyboardOptions = KeyboardOptions(imeAction = ImeAction.Done),
                    keyboardActions = KeyboardActions(onDone = { vm.submit() }),
                    modifier = Modifier.fillMaxWidth(),
                )
            }

            state.error?.let {
                Spacer(Modifier.height(4.dp))
                ErrorBar(it.first, it.second)
            }

            Button(
                onClick = vm::submit,
                enabled = !state.loading && state.userCode.isNotBlank()
                          && state.password.isNotBlank() && langDataAt != null,
                shape = RoundedCornerShape(8.dp),
                colors = ButtonDefaults.buttonColors(containerColor = BrandBlue),
                modifier = Modifier.fillMaxWidth().padding(top = 10.dp).height(46.dp),
            ) {
                if (state.loading) {
                    CircularProgressIndicator(Modifier.size(20.dp), strokeWidth = 2.dp, color = Color.White)
                } else {
                    Icon(Icons.AutoMirrored.Filled.Login, null, tint = Color.White)
                    Spacer(Modifier.width(8.dp))
                    Text(t("login", "LOGIN"), fontSize = 15.sp, fontWeight = FontWeight.Bold, color = Color.White)
                }
            }

            if (hasUpdate) {
                Spacer(Modifier.height(10.dp))
                Button(
                    onClick = { openApkUrl(context, apkUrl!!) },
                    shape = RoundedCornerShape(8.dp),
                    colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFFFC107)),
                    modifier = Modifier.fillMaxWidth().height(42.dp),
                ) {
                    Text(
                        "Update Available (" + serverApk + ")",
                        fontSize = 13.sp,
                        fontWeight = FontWeight.Bold,
                        color = Color(0xFF3D2E00),
                    )
                }
            }

            Spacer(Modifier.height(14.dp))
            if (langDataAt != null) {
                Text(
                    t("htLastUpdated", "Last Updated") + ": " + langDataAt,
                    fontSize = 10.sp,
                    color = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            } else {
                Text(
                    t("htLangDataMissingMsg", "Please download data from the Settings screen."),
                    fontSize = 10.sp,
                    color = Color(0xFFDC3545),
                )
            }

            Spacer(Modifier.height(16.dp))
            LangFlags(selected = lang, onLang = onLang, size = 30)

            Spacer(Modifier.height(10.dp))
            Text(baseUrl, fontSize = 9.sp, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }

        Row(
            Modifier.fillMaxWidth().padding(horizontal = 10.dp),
            horizontalArrangement = Arrangement.End,
        ) {
            IconButton(onClick = onOpenSettings) {
                Icon(
                    Icons.Default.Settings,
                    contentDescription = "Settings",
                    tint = MaterialTheme.colorScheme.onSurfaceVariant,
                )
            }
        }

        HtStatusBar(clock = nowClock(), right = "v${BuildConfig.VERSION_NAME}")
    }

}

@Composable
private fun HtField(label: String, content: @Composable () -> Unit) {
    Column(Modifier.fillMaxWidth().padding(bottom = 14.dp)) {
        Text(
            label,
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.padding(bottom = 4.dp),
        )
        content()
    }
}

fun isNewerVersion(server: String, installed: String): Boolean {
    fun parts(v: String) = v.split(".").map { it.trim().toIntOrNull() ?: 0 }
    val a = parts(server)
    val b = parts(installed)
    for (i in 0 until maxOf(a.size, b.size)) {
        val x = a.getOrElse(i) { 0 }
        val y = b.getOrElse(i) { 0 }
        if (x != y) return x > y
    }
    return false
}

private fun openApkUrl(context: android.content.Context, url: String) {
    runCatching {
        context.startActivity(
            android.content.Intent(android.content.Intent.ACTION_VIEW, android.net.Uri.parse(url)),
        )
    }
}
