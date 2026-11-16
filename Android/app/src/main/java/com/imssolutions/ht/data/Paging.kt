package com.imssolutions.ht.data

object Paging {
    const val PAGE_SIZE = 200
    private const val MAX_PAGES = 100

    suspend fun <T> all(load: suspend (page: Int, pageSize: Int) -> ApiListSuccess<T>): ApiListSuccess<T> {
        val first = load(1, PAGE_SIZE)
        val rows = first.data.toMutableList()
        val total = maxOf(first.meta.total, rows.size)
        var page = 1
        while (rows.size < total && page < MAX_PAGES) {
            page++
            val next = load(page, PAGE_SIZE)
            if (next.data.isEmpty()) break
            rows += next.data
        }
        return first.copy(data = rows, meta = first.meta.copy(total = maxOf(total, rows.size)))
    }
}
