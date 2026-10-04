# Vendor SDKs

The RFID app links against the reader vendors' SDKs, which are not redistributed here.
Obtain them from the vendors and place them in this folder with these exact names:

| File | Reader | Source |
|---|---|---|
| `RfidAPI_V1_1_0.jar` | CipherLab RS36 (RFID Service API 1.1.0) | CipherLab developer resources |
| `DENSOScannerSDK.aar` | DENSO SP1 | DENSO WAVE SP1 SDK |

Both are referenced from `app/build.gradle.kts`. The files are ignored by git.
