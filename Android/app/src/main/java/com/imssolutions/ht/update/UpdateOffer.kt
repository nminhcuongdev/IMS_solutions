package com.imssolutions.ht.update

data class UpdateOffer(
    val entry: VersionEntry,
    val forced: Boolean,
)

data class UpdateDecision(
    val offer: UpdateOffer?,
    val serverDate: Long?,
)
