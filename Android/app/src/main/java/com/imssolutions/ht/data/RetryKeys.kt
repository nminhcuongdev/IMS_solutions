package com.imssolutions.ht.data

class RetryKeys {
    private val keys = mutableMapOf<String, RetryKey>()

    fun of(key: String): RetryKey = keys.getOrPut(key) { RetryKey() }
}
