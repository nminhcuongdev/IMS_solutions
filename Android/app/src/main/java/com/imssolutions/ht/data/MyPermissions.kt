package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class MyPermissions(
    val userCode: String = "",
    val roleCode: String = "",
    val isSuperAdmin: Boolean = false,
    val isBuiltin: Boolean = false,
    val functions: List<PermissionGrant> = emptyList(),
    val visibleFunctions: List<String>? = null,
)
