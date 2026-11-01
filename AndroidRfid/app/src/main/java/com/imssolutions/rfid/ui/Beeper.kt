package com.imssolutions.rfid.ui

import android.media.AudioManager
import android.media.ToneGenerator

class Beeper {
    private val tone: ToneGenerator? =
        runCatching { ToneGenerator(AudioManager.STREAM_MUSIC, VOLUME) }.getOrNull()

    fun ok() {
        runCatching { tone?.startTone(ToneGenerator.TONE_PROP_ACK, OK_MS) }
    }

    private var lastRead = 0L

    fun read() {
        val now = System.currentTimeMillis()
        if (now - lastRead < READ_GAP_MS) return
        lastRead = now
        runCatching { tone?.startTone(ToneGenerator.TONE_PROP_BEEP, READ_MS) }
    }

    fun fail() {
        runCatching { tone?.startTone(ToneGenerator.TONE_PROP_NACK, FAIL_MS) }
    }

    fun release() {
        runCatching { tone?.release() }
    }

    private companion object {
        const val VOLUME = 90
        const val OK_MS = 250
        const val READ_MS = 120
        const val READ_GAP_MS = 150L
        const val FAIL_MS = 400
    }
}
