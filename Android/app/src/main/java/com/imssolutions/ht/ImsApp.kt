package com.imssolutions.ht

import android.app.Application
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.scan.IntentScannerSource
import com.imssolutions.ht.scan.ScannerSource

class ImsApp : Application() {
    override fun onCreate() {
        super.onCreate()
        ApiClient.init(this)
        ApiClient.tokens.forgetSessionIfNotRemembered()

        ScannerSource.active = IntentScannerSource(applicationContext)
    }
}
