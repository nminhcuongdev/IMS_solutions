package com.imssolutions.ht.data

val HT_FEATURES = setOf(
    "ht_receiving", "ht_returns", "ht_picking", "ht_shipping", "ht_inventory", "ht_transfer",
    "ht_prd_receipt", "ht_prd_input", "ht_prd_return", "ht_prd_output", "ht_disposal", "ht_stocktaking",
)

data class HtMenuCategory(val key: String, val labelKey: String, val title: String, val features: List<String>)

val HT_CATEGORIES = listOf(
    HtMenuCategory(
        "warehouse", "htCatWarehouse", "Warehouse",
        listOf("ht_receiving", "ht_returns", "ht_picking", "ht_shipping", "ht_inventory", "ht_transfer"),
    ),
    HtMenuCategory(
        "production", "hpProduction", "Production",
        listOf("ht_prd_receipt", "ht_prd_input", "ht_prd_return", "ht_prd_output"),
    ),
    HtMenuCategory("common", "htCatCommon", "Common", listOf("ht_disposal", "ht_stocktaking")),
)

data class HtMenuAccess(val allowed: Set<String>, val visible: Set<String>)

fun htMenuAccess(permissions: MyPermissions?): HtMenuAccess {
    if (permissions == null) return HtMenuAccess(emptySet(), HT_FEATURES)
    val visible = permissions.visibleFunctions?.filter { it in HT_FEATURES }?.toSet() ?: HT_FEATURES
    val allowed = if (permissions.isSuperAdmin || permissions.isBuiltin) visible
    else permissions.functions
        .filter { it.functionCode in visible && "Open" in it.actions }
        .map { it.functionCode }
        .toSet()
    return HtMenuAccess(allowed, visible)
}

fun htMenuKeys(permissions: MyPermissions?): Set<String> = htMenuAccess(permissions).allowed

fun htMenuEnabled(navKey: String, allowed: Set<String>): Boolean =
    if (navKey == "ht_production") allowed.any { it.startsWith("ht_prd_") } else navKey in allowed

fun htCategoryFeatures(category: HtMenuCategory, access: HtMenuAccess): List<String> =
    category.features.filter { it in access.visible }

fun htCategoryVisible(category: HtMenuCategory, access: HtMenuAccess): Boolean =
    htCategoryFeatures(category, access).isNotEmpty()

fun htCategoryEnabled(category: HtMenuCategory, access: HtMenuAccess): Boolean =
    htCategoryFeatures(category, access).any { it in access.allowed }
