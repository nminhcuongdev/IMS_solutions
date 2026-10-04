package com.imssolutions.ht.ui.vm

import android.app.Application
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.first
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.data.ApiClient
import com.imssolutions.ht.data.LabelStore
import com.imssolutions.ht.data.LanguageRepository
import com.imssolutions.ht.data.resolveLanguage
import com.imssolutions.ht.ui.HtLang
import com.imssolutions.ht.ui.HtStrings

class LanguageViewModel(app: Application) : AndroidViewModel(app) {

    private val store = LabelStore(app)
    private val repo = LanguageRepository(store)

    private val _state = MutableStateFlow(HtStrings())
    val state: StateFlow<HtStrings> = _state.asStateFlow()

    private val _missing = MutableStateFlow(false)
    val missing: StateFlow<Boolean> = _missing.asStateFlow()

    init {
        viewModelScope.launch {
            combine(store.languageOverride, ApiClient.tokens.session) { override, session ->
                resolveLanguage(override, session?.userCode.orEmpty(), session?.defaultLanguage)
            }.distinctUntilChanged().collect { apply(it) }
        }
        viewModelScope.launch {
            if (runCatching { repo.downloadAll() }.getOrDefault(0) > 0) apply(_state.value.lang.code)
        }
    }

    private suspend fun apply(code: String) {
        val labels = repo.labels(code)
        _missing.value = labels.isEmpty()
        _state.value = HtStrings(lang = HtLang.of(code), labels = labels)
    }

    fun pick(lang: HtLang) {
        viewModelScope.launch {
            val who = ApiClient.tokens.session.first()?.userCode.orEmpty()
            store.saveLanguageOverride(lang.code, who)
        }
    }

    suspend fun download(): Int {
        val n = repo.downloadAll()
        apply(_state.value.lang.code)
        return n
    }

    suspend fun clear() {
        repo.clear()
        apply(_state.value.lang.code)
    }
}
