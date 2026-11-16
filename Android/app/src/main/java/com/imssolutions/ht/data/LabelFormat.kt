package com.imssolutions.ht.data

object LabelFormat {
    fun qty(value: Double): String =
        if (value == value.toLong().toDouble()) value.toLong().toString() else value.toString()
}
