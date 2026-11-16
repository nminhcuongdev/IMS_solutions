package com.imssolutions.rfid.ui

import android.app.Application
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.launch
import com.imssolutions.rfid.data.ApiClient
import com.imssolutions.rfid.data.ApiException
import com.imssolutions.rfid.data.LoginRequest
import com.imssolutions.rfid.data.ServerAddress
import com.imssolutions.rfid.data.Prefs
import com.imssolutions.rfid.data.StockScanReadRequest
import com.imssolutions.rfid.data.EpcLookupRequest
import com.imssolutions.rfid.data.EpcRegisterGroup
import com.imssolutions.rfid.data.EpcRegisterRequest
import com.imssolutions.rfid.logic.KnownPart
import com.imssolutions.rfid.logic.PartInput
import com.imssolutions.rfid.logic.RegisterPlan
import com.imssolutions.rfid.logic.groupEpcs
import com.imssolutions.rfid.logic.prefillInputs
import com.imssolutions.rfid.logic.registerPlan
import com.imssolutions.rfid.logic.registrableEpc
import com.imssolutions.rfid.logic.acceptRegisterRead
import com.imssolutions.rfid.logic.withoutGroup
import com.imssolutions.rfid.logic.initialChoice
import com.imssolutions.rfid.logic.keepChosen
import com.imssolutions.rfid.logic.onlyParts
import com.imssolutions.rfid.logic.partChoices
import com.imssolutions.rfid.logic.Board
import com.imssolutions.rfid.logic.ManualSerial
import com.imssolutions.rfid.logic.ScanTag
import com.imssolutions.rfid.logic.SearchHit
import com.imssolutions.rfid.logic.StockScan
import com.imssolutions.rfid.logic.rssiToPercent
import com.imssolutions.rfid.rfid.ReaderState
import com.imssolutions.rfid.rfid.CipherLabReader
import com.imssolutions.rfid.rfid.DensoSp1Reader
import com.imssolutions.rfid.rfid.NewlandMt93Reader
import com.imssolutions.rfid.rfid.ReadMode
import com.imssolutions.rfid.rfid.ReaderKind
import com.imssolutions.rfid.rfid.TagReader
import com.imssolutions.rfid.rfid.TagRead

enum class Screen { Login, Menu, StockParts, StockCount, Search, SearchDetail, Register, Settings }

data class HtDialog(
    val message: String,
    val confirm: Boolean = false,
    val onOk: (() -> Unit)? = null,
)

@OptIn(ExperimentalCoroutinesApi::class)
class AppViewModel(app: Application) : AndroidViewModel(app) {

    val prefs = Prefs(app)
    private val api = ApiClient(prefs)
    private var reader: TagReader = createReader(prefs.readerKind)
    private val readerFlow = MutableStateFlow(reader)
    private var started = false

    var strings by mutableStateOf(Strings(prefs.language)); private set
    var screen by mutableStateOf(Screen.Login); private set
    var userCode by mutableStateOf(""); private set
    var userName by mutableStateOf(""); private set
    var busy by mutableStateOf(false); private set
    var board by mutableStateOf<Board?>(null); private set
    var countBoard by mutableStateOf<Board?>(null); private set
    var stockChoice by mutableStateOf<Set<String>>(emptySet()); private set
    private var lastChoice: Set<String>? = null
    var pending by mutableStateOf<Set<String>>(emptySet()); private set
    var searchItem by mutableStateOf<String?>(null); private set
    var searchRead by mutableStateOf<Map<String, SearchHit>>(emptyMap()); private set
    var readerKind by mutableStateOf(reader.kind); private set
    var powerRange by mutableStateOf(reader.powerRange); private set
    var power by mutableStateOf(initialPower(reader.powerRange)); private set
    var mode by mutableStateOf(ReadMode.Rfid); private set
    var caps by mutableStateOf(reader.caps); private set
    var applying by mutableStateOf<String?>(null); private set
    var scanning by mutableStateOf(false); private set
    var readerState by mutableStateOf(ReaderState.Connecting); private set
    var dialog by mutableStateOf<HtDialog?>(null); private set
    var savedServers by mutableStateOf(prefs.savedServers.ifEmpty { listOf(prefs.serverUrl) }); private set
    var regReads by mutableStateOf<Set<String>>(emptySet()); private set
    var regKnown by mutableStateOf<Map<String, KnownPart>>(emptyMap()); private set
    var regInputs by mutableStateOf<Map<String, PartInput>>(emptyMap()); private set
    var regExcluded by mutableStateOf<Set<String>>(emptySet()); private set
    private val regChecked = mutableSetOf<String>()
    private var lookupJob: Job? = null

    private val t get() = strings
    private val beeper = Beeper()

    val barcodeMode: Boolean get() = mode != ReadMode.Rfid
    val rfidActive: Boolean get() = mode != ReadMode.Barcode
    val softTriggerAvailable: Boolean get() = rfidActive || caps.barcodeBySoftTrigger
    val listensToKeyboard: Boolean
        get() = caps.barcodeByKeyboard && barcodeMode && screen == Screen.StockCount && dialog == null && applying == null

    init {
        if (prefs.rememberMe) {
            prefs.savedSession?.let { (token, code, name) ->
                api.token = token
                userCode = code
                userName = name.ifBlank { code }
                screen = Screen.Menu
            }
        } else {
            prefs.savedSession = null
        }
        viewModelScope.launch { readerFlow.flatMapLatest { it.reads }.collect(::onTag) }
        viewModelScope.launch { readerFlow.flatMapLatest { it.barcodes }.collect(::onBarcode) }
        viewModelScope.launch {
            readerFlow.flatMapLatest { it.state }.collect {
                readerState = it
                if (it == ReaderState.Ready && reader.kind == ReaderKind.CipherLab) restoreSettings()
            }
        }
    }

    private fun initialPower(range: IntRange): Int =
        prefs.txPower.takeIf { it in range } ?: DEFAULT_POWER.coerceIn(range)

    private fun createReader(kind: ReaderKind): TagReader = when (kind) {
        ReaderKind.CipherLab -> CipherLabReader(getApplication())
        ReaderKind.DensoSp1 -> DensoSp1Reader(initialPower(DensoSp1Reader.MIN_POWER..DensoSp1Reader.MAX_POWER))
        ReaderKind.NewlandMt93 -> NewlandMt93Reader(initialPower(NewlandMt93Reader.MIN_POWER..NewlandMt93Reader.MAX_POWER))
    }

    fun onStart() {
        started = true
        reader.start()
    }

    fun restartReader() {
        if (started) reader.start()
    }

    fun onStop() {
        started = false
        scanning = false
        reader.stop()
    }

    fun selectReader(kind: ReaderKind) {
        if (kind == readerKind) return
        stopTrigger()
        reader.release()
        prefs.readerKind = kind
        val next = createReader(kind)
        reader = next
        readerKind = kind
        powerRange = next.powerRange
        caps = next.caps
        mode = ReadMode.Rfid
        power = power.coerceIn(next.powerRange)
        readerFlow.value = next
        if (started) next.start()
    }

    fun setLanguage(lang: String) {
        prefs.language = lang
        strings = Strings(lang)
    }

    fun alert(message: String) {
        dialog = HtDialog(message)
    }

    private fun confirm(message: String, onOk: () -> Unit) {
        dialog = HtDialog(message, confirm = true, onOk = onOk)
    }

    fun dialogOk() {
        val cb = dialog?.onOk
        dialog = null
        cb?.invoke()
    }

    fun dialogCancel() {
        dialog = null
    }

    private var settingsReturn = Screen.Login

    fun openSettings() {
        if (screen == Screen.Settings) return
        stopTrigger()
        settingsReturn = screen
        screen = Screen.Settings
    }

    fun backFromSettings() {
        screen = settingsReturn
    }

    private val deviceIp = ServerAddress.deviceIpv4()

    fun resolveServer(raw: String): List<String> = ServerAddress.candidates(raw, deviceIp)

    fun saveServer(raw: String, onSaved: (String) -> Unit) {
        if (raw.isBlank()) {
            alert(t.t("errServerRequired"))
            return
        }
        val candidates = resolveServer(raw)
        if (candidates.isEmpty()) {
            alert(t.t("errServerInvalid"))
            return
        }
        if (busy) return
        busy = true
        viewModelScope.launch {
            val found = try {
                candidates.firstOrNull { api.probe(it) }
            } finally {
                busy = false
            }
            if (found == null) {
                alert(t.t("errServerUnreachable", candidates.joinToString(", ") { ServerAddress.toDisplay(it) }))
                return@launch
            }
            onSaved(applyServer(found))
        }
    }

    private fun applyServer(server: String): String {
        val previous = prefs.serverUrl
        val switched = !ServerAddress.sameServer(server, previous)
        val known = prefs.savedServers.ifEmpty { listOf(previous) }
        prefs.serverUrl = server
        prefs.savedServers = ServerAddress.remember(known, server)
        savedServers = prefs.savedServers
        if (switched && api.token != null) {
            api.token = null
            prefs.savedSession = null
            resetWork()
            settingsReturn = Screen.Login
            alert(t.t("msgServerSwitched", ServerAddress.toDisplay(server)))
        } else {
            alert(t.t("msgServerSaved"))
        }
        return ServerAddress.toDisplay(server)
    }

    fun forgetServer(baseUrl: String) {
        prefs.savedServers = prefs.savedServers.filter { !ServerAddress.sameServer(it, baseUrl) }
        savedServers = prefs.savedServers
    }

    fun login(userId: String, password: String, remember: Boolean) {
        if (userId.isBlank() || password.isEmpty()) {
            alert(t.t("errUserRequired"))
            return
        }
        launchBusy {
            val res = api.call { login(LoginRequest(userId.trim(), password)) }.data
                ?: throw ApiException("HTTP-500", 500, "")
            api.token = res.token
            userCode = res.userCode
            userName = res.userName.ifBlank { res.userCode }
            prefs.rememberMe = remember
            prefs.lastUserId = if (remember) userId.trim() else ""
            prefs.savedSession = if (remember) Triple(res.token, userCode, userName) else null
            screen = Screen.Menu
        }
    }

    fun logout() {
        val hadToken = api.token != null
        viewModelScope.launch {
            if (hadToken) runCatching { api.call { logout() } }
            api.token = null
        }
        prefs.savedSession = null
        resetWork()
        screen = Screen.Login
    }

    private fun resetWork() {
        board = null
        pending = emptySet()
        searchItem = null
        searchRead = emptyMap()
        clearRegisterState()
        countBoard = null
        stockChoice = emptySet()
        lastChoice = null
        stopTrigger()
    }

    fun openStockCount() {
        pending = emptySet()
        countBoard = null
        stockChoice = emptySet()
        screen = Screen.StockParts
        loadBoard()
    }

    fun toggleStockPart(key: String) {
        stockChoice = if (key in stockChoice) stockChoice - key else stockChoice + key
    }

    fun setAllStockParts(all: Boolean) {
        val b = board ?: return
        stockChoice = if (all) partChoices(b).map { it.key }.toSet() else emptySet()
    }

    fun startStockCount() {
        val b = board ?: return
        if (stockChoice.isEmpty()) {
            alert(t.t("errNoPartChosen"))
            return
        }
        lastChoice = stockChoice
        pending = keepChosen(b, pending, stockChoice)
        countBoard = b.onlyParts(stockChoice)
        screen = Screen.StockCount
    }

    fun backFromStockCount() {
        stopTrigger()
        screen = Screen.StockParts
    }

    fun backFromStockParts() {
        if (pending.isEmpty()) {
            leaveStockCount()
            return
        }
        confirm(t.t("confirmDiscard", pending.size)) { leaveStockCount() }
    }

    private fun leaveStockCount() {
        stopTrigger()
        pending = emptySet()
        countBoard = null
        screen = Screen.Menu
    }

    fun saveStockCount() {
        confirm(t.t("confirmSaveStockCount")) {
            val epcs = pending.toList()
            if (epcs.isEmpty()) {
                alert(t.t("msgNothingToSave"))
                return@confirm
            }
            launchBusy {
                api.call { read(StockScanReadRequest(epcs)) }
                stopTrigger()
                pending = emptySet()
                board = null
                countBoard = null
                screen = Screen.Menu
                alert(t.t("msgSaved"))
            }
        }
    }

    fun openSearch() {
        if (mode != ReadMode.Rfid) changeMode(ReadMode.Rfid, quiet = true)
        screen = Screen.Search
        loadBoard()
    }

    fun backToMenu() {
        stopTrigger()
        screen = Screen.Menu
    }

    fun selectSearchItem(name: String) {
        if (name == searchItem) return
        searchItem = name
        searchRead = emptyMap()
    }

    fun addSerial(raw: String): Boolean {
        val item = searchItem ?: run {
            alert(t.t("errSelectProduct"))
            return false
        }
        val b = board ?: return false
        return when (val r = b.manualSerial(item, raw)) {
            ManualSerial.Blank -> {
                alert(t.t("errSerialRequired"))
                false
            }
            ManualSerial.NotFound -> {
                alert(t.t("errSerialNotFound", raw.trim()))
                false
            }
            ManualSerial.AlreadyScanned -> {
                alert(t.t("errSerialScanned", raw.trim()))
                true
            }
            is ManualSerial.Read -> {
                searchRead = searchRead + (r.epc to SearchHit(100, System.currentTimeMillis(), manual = true))
                true
            }
        }
    }

    fun clearSearch() {
        confirm(t.t("confirmClear")) { searchRead = emptyMap() }
    }

    fun openSearchDetail() {
        if (searchItem == null) {
            alert(t.t("errSelectProduct"))
            return
        }
        screen = Screen.SearchDetail
    }

    fun backFromSearchDetail() {
        screen = Screen.Search
    }

    private fun restoreSettings() {
        viewModelScope.launch {
            reader.setPower(power)
            if (mode != ReadMode.Rfid) reader.setMode(mode)
        }
    }

    fun changePower(value: Int) {
        if (applying != null || value == power && readerState == ReaderState.Ready) return
        stopTrigger()
        val previous = power
        power = value
        prefs.txPower = value
        applying = t.t("htApplyingPower", value)
        viewModelScope.launch {
            val err = reader.setPower(value)
            applying = null
            when {
                err == null -> beeper.ok()
                readerState != ReaderState.Ready -> {
                    beeper.fail()
                    alert(t.t("msgSettingPending", readerKind.label))
                }
                else -> {
                    power = previous
                    prefs.txPower = previous
                    beeper.fail()
                    alert(t.t("rdError", err))
                }
            }
        }
    }

    fun changeMode(next: ReadMode, quiet: Boolean = false) {
        if (applying != null || next == mode || next !in caps.modes) return
        stopTrigger()
        val previous = mode
        mode = next
        applying = t.t("htSwitchingMode", modeLabel(next))
        viewModelScope.launch {
            val err = reader.setMode(next)
            applying = null
            when {
                err == null -> if (!quiet) beeper.ok()
                readerState != ReaderState.Ready -> {
                    beeper.fail()
                    alert(t.t("msgSettingPending", readerKind.label))
                }
                else -> {
                    mode = previous
                    beeper.fail()
                    alert(t.t("rdError", err))
                }
            }
        }
    }

    fun toggleMode() {
        val modes = caps.modes
        changeMode(modes[(modes.indexOf(mode) + 1) % modes.size])
    }

    fun modeLabel(m: ReadMode): String = when (m) {
        ReadMode.Rfid -> "RFID"
        ReadMode.Barcode -> t.t("htModeBarcode")
        ReadMode.Both -> t.t("htModeBoth")
    }

    fun onBarcode(raw: String) {
        if (screen != Screen.StockCount || !barcodeMode) return
        val b = countBoard ?: return
        val code = raw.trim()
        when (val r = b.stockScan(pending, code)) {
            is StockScan.Counted -> {
                pending = pending + r.epc
                beeper.read()
            }
            StockScan.AlreadyCounted -> {
                beeper.fail()
                alert(t.t("errBarcodeCounted", code))
            }
            StockScan.Unknown -> {
                beeper.fail()
                alert(t.t("errBarcodeUnknown", code))
            }
        }
    }

    fun startTrigger() {
        if (scanning || applying != null) return
        val err = reader.trigger(true)
        if (err != null) {
            alert(t.t("rdError", err))
            return
        }
        scanning = true
    }

    fun stopTrigger() {
        if (!scanning) return
        scanning = false
        reader.trigger(false)
    }

    fun openRegister() {
        if (mode != ReadMode.Rfid) changeMode(ReadMode.Rfid, quiet = true)
        clearRegisterState()
        screen = Screen.Register
    }

    private fun clearRegisterState() {
        lookupJob?.cancel()
        lookupJob = null
        regReads = emptySet()
        regKnown = emptyMap()
        regInputs = emptyMap()
        regExcluded = emptySet()
        regChecked.clear()
    }

    private fun unsavedRegisterCount(): Int =
        groupEpcs(regReads, regKnown).sumOf { it.newEpcs.size }

    fun backFromRegister() {
        val unsaved = unsavedRegisterCount()
        if (unsaved == 0) {
            leaveRegister()
            return
        }
        confirm(t.t("confirmDiscard", unsaved)) { leaveRegister() }
    }

    private fun leaveRegister() {
        stopTrigger()
        clearRegisterState()
        screen = Screen.Menu
    }

    fun clearRegister() {
        if (regReads.isEmpty() && regExcluded.isEmpty()) return
        confirm(t.t("confirmClear")) {
            stopTrigger()
            clearRegisterState()
        }
    }

    fun setPartInput(prefix: String, input: PartInput) {
        regInputs = regInputs + (prefix to input)
    }

    fun excludeGroup(prefix: String) {
        regExcluded = regExcluded + prefix
        regReads = withoutGroup(regReads, prefix)
        regInputs = regInputs - prefix
    }

    fun restoreExcluded() {
        regExcluded = emptySet()
    }

    fun saveRegister() {
        stopTrigger()
        when (val plan = registerPlan(groupEpcs(regReads, regKnown), regInputs)) {
            RegisterPlan.Nothing -> alert(t.t("msgNothingToRegister"))
            is RegisterPlan.Missing -> alert(t.t("errPartRequired", plan.prefix))
            is RegisterPlan.TooLong -> alert(t.t("errPartTooLong", plan.prefix))
            is RegisterPlan.Ready -> confirm(t.t("confirmRegister", plan.tagCount, plan.partCount)) {
                stopTrigger()
                launchBusy {
                    val body = EpcRegisterRequest(plan.lines.map { EpcRegisterGroup(it.code, it.name, it.epcs) })
                    val res = try {
                        api.call { registerEpcs(body) }.data
                    } catch (e: ApiException) {
                        if (e.code != "DUP-002") throw e
                        regChecked.clear()
                        scheduleLookup(0)
                        alert(t.t("errEpcRegistered"))
                        return@launchBusy
                    }
                    val sent = plan.lines.flatMap { it.epcs }.toSet()
                    val left = regReads.filter { it !in sent && it !in regKnown }.toSet()
                    clearRegisterState()
                    if (left.isNotEmpty()) {
                        regReads = left
                        scheduleLookup(0)
                    }
                    board = null
                    alert(t.t("msgRegistered", res?.registered ?: plan.tagCount))
                }
            }
        }
    }

    private fun onRegisterRead(raw: String?) {
        val epc = acceptRegisterRead(regReads, regExcluded, raw) ?: return
        regReads = regReads + epc
        if (caps.beepOnRead) beeper.read()
        scheduleLookup(LOOKUP_DELAY_MS)
    }

    private fun scheduleLookup(delayMs: Long) {
        if (lookupJob?.isActive == true) return
        lookupJob = viewModelScope.launch {
            delay(delayMs)
            while (screen == Screen.Register) {
                val batch = (regReads - regChecked).toList()
                if (batch.isEmpty()) break
                val data = try {
                    api.call { lookupEpcs(EpcLookupRequest(batch)) }.data
                } catch (e: ApiException) {
                    break
                } ?: break
                regChecked += batch
                regKnown = regKnown + data.registered.associate { it.epc to KnownPart(it.itemCode, it.itemName) }
                regInputs = prefillInputs(regInputs, data.prefixes.associate { it.prefix to KnownPart(it.itemCode, it.itemName) })
            }
        }
    }

    private fun onTag(read: TagRead) {
        if (screen == Screen.Register) {
            if (rfidActive) onRegisterRead(read.epc)
            return
        }
        val b = board ?: return
        when (screen) {
            Screen.StockCount -> if (rfidActive) {
                countBoard?.acceptStockRead(pending, read.epc)?.let {
                    pending = pending + it
                    if (caps.beepOnRead) beeper.read()
                }
            }
            Screen.Search, Screen.SearchDetail ->
                b.acceptSearchRead(searchItem, read.epc)?.let {
                    if (searchRead[it]?.manual != true) {
                        searchRead = searchRead + (it to SearchHit(rssiToPercent(read.rssi), System.currentTimeMillis()))
                        if (caps.beepOnRead) beeper.read()
                    }
                }
            else -> Unit
        }
    }

    fun loadBoard() {
        launchBusy {
            val dto = api.call { stockScan() }.data ?: return@launchBusy
            val b = Board(dto.rows.map { ScanTag(it.epc, it.itemCode, it.itemName, it.isScanned) })
            board = b
            if (screen == Screen.StockParts) stockChoice = initialChoice(partChoices(b), lastChoice)
            val names = b.itemNames()
            val current = searchItem
            if (current == null || current !in names) {
                searchItem = names.firstOrNull()
                searchRead = emptyMap()
            } else {
                searchRead = searchRead.filterKeys { b.acceptSearchRead(current, it) != null }
            }
        }
    }

    private fun launchBusy(block: suspend () -> Unit) {
        if (busy) return
        busy = true
        viewModelScope.launch {
            try {
                block()
            } catch (e: ApiException) {
                onApiError(e)
            } finally {
                busy = false
            }
        }
    }

    private fun onApiError(e: ApiException) {
        when {
            e.code == "NET-002" -> alert(t.t("errNetwork"))
            e.isUnauthorized && screen != Screen.Login -> {
                api.token = null
                prefs.savedSession = null
                resetWork()
                screen = Screen.Login
                alert(t.t("errSession"))
            }
            else -> alert(listOf(e.code, e.message).filter { it.isNotBlank() }.joinToString("\n"))
        }
    }

    override fun onCleared() {
        beeper.release()
        reader.release()
    }

    private companion object {
        const val DEFAULT_POWER = 30
        const val LOOKUP_DELAY_MS = 400L
    }
}
