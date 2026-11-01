package com.imssolutions.ht.data

import java.util.UUID

class RetryKey(private val newId: () -> String = { UUID.randomUUID().toString() }) {
    private var body: Any? = null
    private var id: String? = null

    fun idFor(request: Any): String {
        val current = id
        if (current != null && request == body) return current
        body = request
        return newId().also { id = it }
    }

    fun succeeded() {
        body = null
        id = null
    }

    fun failed(error: Throwable) {
        if (error is ApiException && error.httpStatus == 0) return
        succeeded()
    }
}
