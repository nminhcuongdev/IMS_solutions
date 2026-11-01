package com.imssolutions.ht.update

import kotlinx.serialization.Serializable

@Serializable
data class UpdateTargets(
    val all: Boolean = true,
    val deviceIds: List<String> = emptyList(),
)

@Serializable
data class VersionEntry(
    val packageName: String = "",
    val versionCode: Long = 0,
    val versionName: String = "",
    val updatedAt: String? = null,
    val apkFile: String = "",
    val sha256: String = "",
    val minVersionCode: Long? = null,
    val force: Boolean = false,
    val notes: Map<String, String> = emptyMap(),
    val targets: UpdateTargets? = null,
)

@Serializable
data class VersionJson(
    val apps: List<VersionEntry> = emptyList(),
)
