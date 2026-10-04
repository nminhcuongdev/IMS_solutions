package com.imssolutions.rfid

import android.Manifest
import android.content.pm.PackageManager
import android.os.Build
import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.result.contract.ActivityResultContracts
import androidx.activity.viewModels
import androidx.compose.material3.MaterialTheme
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.LaunchedEffect
import androidx.core.content.ContextCompat
import com.imssolutions.rfid.rfid.ReaderKind
import com.imssolutions.rfid.ui.AppViewModel
import com.imssolutions.rfid.ui.HtApp
import com.imssolutions.rfid.ui.LocalStrings

class MainActivity : ComponentActivity() {

    private val vm: AppViewModel by viewModels()

    private val bluetoothPermissions: Array<String> =
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S) {
            arrayOf(Manifest.permission.BLUETOOTH_CONNECT, Manifest.permission.BLUETOOTH_SCAN)
        } else {
            emptyArray()
        }

    private val askBluetooth = registerForActivityResult(ActivityResultContracts.RequestMultiplePermissions()) { result ->
        if (result.values.all { it }) vm.restartReader() else vm.alert(vm.strings.t("rdNoPermission"))
    }

    private fun missingBluetooth(): Array<String> = bluetoothPermissions.filter {
        ContextCompat.checkSelfPermission(this, it) != PackageManager.PERMISSION_GRANTED
    }.toTypedArray()

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            LaunchedEffect(vm.readerKind) {
                if (vm.readerKind == ReaderKind.DensoSp1) {
                    val missing = missingBluetooth()
                    if (missing.isNotEmpty()) askBluetooth.launch(missing)
                }
            }
            MaterialTheme {
                CompositionLocalProvider(LocalStrings provides vm.strings) {
                    HtApp(vm)
                }
            }
        }
    }

    override fun onStart() {
        super.onStart()
        if (vm.readerKind == ReaderKind.DensoSp1 && missingBluetooth().isNotEmpty()) return
        vm.onStart()
    }

    override fun onStop() {
        vm.onStop()
        super.onStop()
    }
}
