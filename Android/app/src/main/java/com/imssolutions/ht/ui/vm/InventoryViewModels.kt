package com.imssolutions.ht.ui.vm

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.launch
import com.imssolutions.ht.data.InventoryRepository
import com.imssolutions.ht.data.StockGroup
import com.imssolutions.ht.data.StockLine


private fun Throwable.toPair(): Pair<String, String> = when (this) {
    is com.imssolutions.ht.data.ApiException -> code to message
    else -> "ERR" to (message ?: toString())
}


enum class StockFilter(val labelKey: String, val label: String) {
    ALL("htFilterAll", "All"),
    ALERT("htFilterStockAlert", "Low + On Hold"),
    EXPIRING("htFilterStockExpiring", "Expiring"),
}

data class StockListState(
    val loading: Boolean = true,
    val items: List<StockGroup> = emptyList(),
    val filter: StockFilter = StockFilter.ALL,
    val query: String = "",
    val error: Pair<String, String>? = null,
) {
    val visible: List<StockGroup> = items.filter { g ->
        when (filter) {
            StockFilter.ALL -> true
            StockFilter.ALERT -> g.isAlert
            StockFilter.EXPIRING -> g.expiry != com.imssolutions.ht.data.ExpiryState.NONE
        }
    }.filter {
        val q = query.trim()
        q.isEmpty() || it.key.contains(q, ignoreCase = true) ||
            it.name.orEmpty().contains(q, ignoreCase = true)
    }
}

class StockListViewModel : ViewModel() {
    private val repo = InventoryRepository()
    private val _state = MutableStateFlow(StockListState())
    val state: StateFlow<StockListState> = _state.asStateFlow()

    init { load() }

    fun setFilter(v: StockFilter) = _state.update { it.copy(filter = v) }
    fun setQuery(v: String) = _state.update { it.copy(query = v) }

    fun load() {
        _state.update { it.copy(loading = true, error = null) }
        viewModelScope.launch {
            runCatching { repo.listItems() }
                .onSuccess { r -> _state.update { it.copy(loading = false, items = r.data) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }
}


data class StockDetailState(
    val loading: Boolean = true,
    val itemCode: String = "",
    val lines: List<StockLine> = emptyList(),
    val error: Pair<String, String>? = null,
) {
    val head: StockLine? get() = lines.firstOrNull()

    val totalQty: Double get() = lines.sumOf { it.stockQty }

    val anyOnHold: Boolean get() = lines.any { it.isOnHold }

    val holdReason: String? get() = lines.firstOrNull { it.isOnHold }?.holdReason

    val empty: Boolean get() = !loading && lines.isEmpty()
}

class StockDetailViewModel : ViewModel() {
    private val repo = InventoryRepository()
    private val _state = MutableStateFlow(StockDetailState())
    val state: StateFlow<StockDetailState> = _state.asStateFlow()

    fun load(itemCode: String) {
        _state.update { it.copy(loading = true, itemCode = itemCode, error = null) }
        viewModelScope.launch {
            runCatching { repo.linesOf(itemCode) }
                .onSuccess { rows -> _state.update { it.copy(loading = false, lines = rows) } }
                .onFailure { e -> _state.update { it.copy(loading = false, error = e.toPair()) } }
        }
    }
}
