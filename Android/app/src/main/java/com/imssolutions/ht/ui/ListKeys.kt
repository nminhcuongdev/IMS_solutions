package com.imssolutions.ht.ui

fun distinctKeys(keys: List<String>): List<String> {
    val used = HashSet<String>()
    return keys.map { key ->
        var candidate = key
        var n = 1
        while (!used.add(candidate)) candidate = "$key#${++n}"
        candidate
    }
}
