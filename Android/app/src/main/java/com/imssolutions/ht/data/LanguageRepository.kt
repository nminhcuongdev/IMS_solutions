package com.imssolutions.ht.data

class LanguageRepository(private val store: LabelStore) {

    suspend fun downloadAll(): Int {
        var total = 0
        for (lang in LANGS) {
            val map = ApiClient.call { languageLabels(lang = lang, deviceScope = "HT") }.data.orEmpty()
            if (map.isNotEmpty()) {
                store.save(lang, map)
                total += map.size
            }
        }
        return total
    }

    suspend fun labels(lang: String): Map<String, String> = store.load(lang)

    suspend fun clear() = store.clear()

    companion object {
        val LANGS = listOf("en", "ja", "vi")
    }
}

fun resolveLanguage(
    override: Pair<String, String>?,
    userCode: String,
    userDefault: String?,
): String {
    val lang = override?.first
    val owner = override?.second.orEmpty()
    val ownedByCurrentUser = lang != null && (userCode.isEmpty() || owner == userCode)
    if (ownedByCurrentUser) return lang!!

    val declared = userDefault?.trim()?.takeIf { it in LanguageRepository.LANGS }
    if (declared != null) return declared

    if (lang != null && owner.isEmpty()) return lang
    return "vi"
}
