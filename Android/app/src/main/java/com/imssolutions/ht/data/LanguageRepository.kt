package com.imssolutions.ht.data

import com.imssolutions.ht.ui.HtLang

class LanguageRepository(private val store: LabelStore) {

    suspend fun downloadAll(): Int {
        val offered = ApiClient.call { languages() }.data.orEmpty()
        if (offered.isNotEmpty()) store.saveLanguages(offered)
        var total = 0
        for (lang in languages().map { it.code }) {
            val map = ApiClient.call { languageLabels(lang = lang, deviceScope = "HT") }.data.orEmpty()
            if (map.isNotEmpty()) {
                store.save(lang, map)
                total += map.size
            }
        }
        return total
    }

    suspend fun languages(): List<HtLang> =
        store.loadLanguages()
            .map { HtLang(it.languageCode.trim().lowercase(), it.languageName, it.iconImage) }
            .ifEmpty { HtLang.BUILT_IN }

    suspend fun labels(lang: String): Map<String, String> = store.load(lang)

    suspend fun clear() = store.clear(languages().map { it.code })

    companion object {
        val DEFAULT_CODES: Set<String> = HtLang.BUILT_IN.map { it.code }.toSet()
    }
}

val DEVICE_LANGUAGES = setOf("en", "ja", "vi")

fun resolveLanguage(
    override: Pair<String, String>?,
    userCode: String,
    userDefault: String?,
    known: Set<String> = LanguageRepository.DEFAULT_CODES,
    deviceLanguage: String? = null,
): String {
    val lang = override?.first?.takeIf { it in known }
    val owner = override?.second.orEmpty()
    val ownedByCurrentUser = lang != null && (userCode.isEmpty() || owner == userCode)
    if (ownedByCurrentUser) return lang!!

    val declared = userDefault?.trim()?.lowercase()?.takeIf { it in known }
    if (declared != null) return declared

    if (lang != null && owner.isEmpty()) return lang
    val device = deviceLanguage?.trim()?.lowercase()?.takeIf { it in DEVICE_LANGUAGES && it in known }
    if (device != null) return device
    return if (HtLang.EN.code in known) HtLang.EN.code else known.firstOrNull() ?: HtLang.EN.code
}
