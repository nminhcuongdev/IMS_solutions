package com.imssolutions.ht.ui

import android.media.AudioManager
import android.media.ToneGenerator
import android.os.Handler
import android.os.Looper
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect

fun playErrorSound() = playTone(ToneGenerator.TONE_SUP_ERROR, 600)

@Composable
fun ErrorSound(error: Any?) {
    LaunchedEffect(error) { if (error != null) playErrorSound() }
}

private fun playTone(tone: Int, durationMs: Int) {
    runCatching {
        val gen = ToneGenerator(AudioManager.STREAM_MUSIC, 90)
        gen.startTone(tone, durationMs)
        Handler(Looper.getMainLooper()).postDelayed({ runCatching { gen.release() } }, durationMs + 250L)
    }
}

fun playSuccessSound() = playTone(ToneGenerator.TONE_PROP_ACK, 250)
