package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class MyPermissions(
    val userCode: String = "",
    val isSuperAdmin: Boolean = false,
    val functions: List<PermissionGrant> = emptyList(),
)
