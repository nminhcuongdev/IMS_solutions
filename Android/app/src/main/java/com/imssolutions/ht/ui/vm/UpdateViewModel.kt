package com.imssolutions.ht.ui.vm

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import com.imssolutions.ht.BuildConfig
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.ui.HtMsg
import com.imssolutions.ht.update.ApkInstaller
import com.imssolutions.ht.update.ApkVerifier
import com.imssolutions.ht.update.UpdateClient
import com.imssolutions.ht.update.UpdateInstallReceiver
import com.imssolutions.ht.update.UpdateOffer
import com.imssolutions.ht.update.UpdateRules
import java.io.File
import java.io.IOException

enum class UpdatePhase { OFFER, DOWNLOADING, VERIFYING, PERMISSION, INSTALLING, FAILED }

data class UpdateUiState(
    val offer: UpdateOffer? = null,
    val dialog: Boolean = false,
    val phase: UpdatePhase = UpdatePhase.OFFER,
    val done: Long = 0,
    val total: Long = 0,
    val error: HtMsg? = null,
    val notice: HtMsg? = null,
    val checking: Boolean = false,
)

class UpdateViewModel(app: Application) : AndroidViewModel(app) {

    private class NoSpaceException(val needed: Long) : IOException()

    private val _state = MutableStateFlow(UpdateUiState())
    val state: StateFlow<UpdateUiState> = _state.asStateFlow()

    val installedCode: Long = BuildConfig.VERSION_CODE.toLong()
    val installedName: String = BuildConfig.VERSION_NAME

    private var shownThisLaunch = false

    init {
        viewModelScope.launch {
            reportOutcome()
            check(manual = false)
        }
        viewModelScope.launch {
            UpdateInstallReceiver.failures.collect { message ->
                fail(HtMsg("htUpdInstallFailed", "Không thể cài đặt: {0}", listOf(message)))
            }
        }
    }

    private suspend fun reportOutcome() {
        val store = ApiClient.updates
        val s = store.current()
        val started = s.startedCode
        if (started != null) {
            val notice = when {
                installedCode >= started -> HtMsg("htUpdDone", "Đã cập nhật lên phiên bản {0}.", listOf(s.startedName ?: installedName))
                s.failure != null -> HtMsg("htUpdInstallFailed", "Không thể cài đặt: {0}", listOf(s.failure))
                else -> null
            }
            _state.update { it.copy(notice = notice) }
        }
        store.clearOutcome(installedCode)
    }

    fun check(manual: Boolean) {
        if (_state.value.checking) return
        viewModelScope.launch { runCheck(manual) }
    }

    private suspend fun runCheck(manual: Boolean) {
        val store = ApiClient.updates
        val s = store.current()
        val deviceId = UpdateRules.deviceIdOf(s.deviceId)
        if (deviceId == null) {
            if (manual) notice(HtMsg("htUpdNeedDeviceId", "Hãy nhập Mã thiết bị (Device Code) trước."))
            return
        }
        val base = ApiClient.tokens.baseUrl.first()
        _state.update { it.copy(checking = true) }
        val fetched = runCatching {
            withContext(Dispatchers.IO) { UpdateClient(base, deviceId, installedCode).fetchVersionJson() }
        }
        _state.update { it.copy(checking = false) }
        val f = fetched.getOrElse { e ->
            if (manual) notice(
                if (e is IOException) HtMsg("NET-002", "Không kết nối được máy chủ. Kiểm tra Wi-Fi.")
                else HtMsg("htUpdCheckFailed", "Không đọc được thông tin cập nhật.")
            )
            return
        }
        val d = UpdateRules.decide(
            f.doc, getApplication<Application>().packageName, deviceId, installedCode,
            f.lastModified, s.lastSavedDate, s.pendingCode,
        )
        store.saveCheck(d.serverDate, d.offer?.entry?.versionCode)
        val offer = d.offer
        if (offer == null) {
            _state.update { if (it.phase == UpdatePhase.OFFER) it.copy(offer = null, dialog = false) else it }
            if (manual) notice(HtMsg("htUpdLatest", "Đang dùng phiên bản mới nhất."))
            return
        }
        val show = manual || !shownThisLaunch || offer.forced
        shownThisLaunch = true
        _state.update {
            if (it.phase == UpdatePhase.OFFER || it.phase == UpdatePhase.FAILED) {
                it.copy(offer = offer, dialog = it.dialog || show, phase = UpdatePhase.OFFER, error = null)
            } else it
        }
    }

    fun open() = _state.update { if (it.offer != null) it.copy(dialog = true) else it }

    fun later() = _state.update {
        if (it.offer?.forced == true || it.phase == UpdatePhase.DOWNLOADING || it.phase == UpdatePhase.VERIFYING) it
        else it.copy(dialog = false, phase = UpdatePhase.OFFER, error = null)
    }

    fun clearNotice() = _state.update { it.copy(notice = null) }

    private fun notice(m: HtMsg) = _state.update { it.copy(notice = m) }

    private fun fail(m: HtMsg) = _state.update { it.copy(phase = UpdatePhase.FAILED, error = m, dialog = it.offer != null) }

    fun update() {
        val offer = _state.value.offer ?: return
        val phase = _state.value.phase
        if (phase == UpdatePhase.DOWNLOADING || phase == UpdatePhase.VERIFYING) return
        val ctx = getApplication<Application>()
        if (!ApkInstaller.canInstall(ctx)) {
            _state.update { it.copy(phase = UpdatePhase.PERMISSION, error = null) }
            return
        }
        val e = offer.entry
        viewModelScope.launch {
            val deviceId = UpdateRules.deviceIdOf(ApiClient.updates.current().deviceId).orEmpty()
            val base = ApiClient.tokens.baseUrl.first()
            val dir = File(ctx.filesDir, "update").apply { mkdirs() }
            val part = File(dir, e.sha256.trim().lowercase().take(16) + ".apk")
            dir.listFiles()?.filter { it != part }?.forEach { it.delete() }
            _state.update { it.copy(phase = UpdatePhase.DOWNLOADING, error = null, done = part.length(), total = 0) }
            try {
                withContext(Dispatchers.IO) {
                    val client = UpdateClient(base, deviceId, installedCode)
                    val total = client.totalSize(e.apkFile)
                    if (total != null && part.length() > total) part.delete()
                    if (total == null || part.length() < total) {
                        if (total != null) {
                            val need = UpdateRules.spaceNeeded(total, part.length())
                            if (dir.usableSpace < need) throw NoSpaceException(need)
                        }
                        client.download(e.apkFile, part) { d, t -> _state.update { it.copy(done = d, total = t) } }
                    } else {
                        _state.update { it.copy(done = total, total = total) }
                    }
                }
                _state.update { it.copy(phase = UpdatePhase.VERIFYING) }
                val failure = withContext(Dispatchers.IO) { ApkVerifier.verify(ctx, part, e) }
                if (failure != null) {
                    part.delete()
                    fail(
                        if (failure == ApkVerifier.Failure.HASH) HtMsg("htUpdHashMismatch", "Tệp tải về bị hỏng hoặc đã bị thay đổi. Hãy thử lại.")
                        else HtMsg("htUpdVerifyFailed", "Tệp cập nhật không khớp với ứng dụng này. Hãy liên hệ quản trị viên.")
                    )
                    return@launch
                }
                _state.update { it.copy(phase = UpdatePhase.INSTALLING) }
                ApiClient.updates.saveStarted(e.versionCode, e.versionName)
                withContext(Dispatchers.IO) { ApkInstaller.install(ctx, part) }
            } catch (x: NoSpaceException) {
                fail(HtMsg("htUpdNoSpace", "Không đủ dung lượng trống. Cần {0} MB.", listOf((x.needed + 1048575) / 1048576)))
            } catch (x: IOException) {
                fail(HtMsg("htUpdDownloadFailed", "Tải xuống thất bại: {0}", listOf(x.message ?: x.javaClass.simpleName)))
            } catch (x: Exception) {
                fail(HtMsg("htUpdInstallFailed", "Không thể cài đặt: {0}", listOf(x.message ?: x.javaClass.simpleName)))
            }
        }
    }
}
