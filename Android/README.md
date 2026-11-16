# IMS HT app (Android)

Kotlin + Jetpack Compose app for handheld terminals with a built-in barcode / QR scanner (Android 7.0+, target 35).

## Build

Requirements: Android Studio, or JDK 17 + Android SDK 35.

```bash
./gradlew assembleDebug testDebugUnitTest
```

For a device on USB, `adb reverse tcp:8091 tcp:8091` lets it reach an API running on your PC.

## What it does

| Menu | Screens |
|---|---|
| Receiving / Returns | Pick a slip and a line, scan the product label and the location label, confirm one package at a time, print the label |
| Picking / Shipping | Walk to the planned location, scan the product label for the lot, confirm the line |
| Production | Material receipt, consumption by scanning the process and material labels, leftover return, output with lot and expiry (left blank, the server dates it), finished-goods label |
| Inventory | Stock inquiry, location transfer (destination first, then scan cartons), disposal |
| Stocktaking | Count by shelf; only labels the snapshot records at that shelf are accepted |
| Settings | Server address with saved servers and port auto-detection, printer, default zone, language data download, app update |

## Notes

- Screen text comes from the server's `language_data` (`t(key, fallback)`), cached on the device so it works offline;
  before sign-in the device language (English, Japanese or Vietnamese) is used.
- Menus are greyed out by the signed-in user's permissions.
- Every write carries a request id, so a confirmation retried after a lost Wi-Fi reply is recorded once.
- The app updates itself from the API's data distribution rules (`/dl/version.json`), always asking the user before
  installing.
- The scanner is read through the vendor's broadcast intent; another intent action can be configured in
  `scan/IntentScannerSource.kt`.
