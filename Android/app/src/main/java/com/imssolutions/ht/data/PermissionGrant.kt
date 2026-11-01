package com.imssolutions.ht.data

import kotlinx.serialization.Serializable

@Serializable
data class PermissionGrant(val functionCode: String = "", val actions: List<String> = emptyList())
