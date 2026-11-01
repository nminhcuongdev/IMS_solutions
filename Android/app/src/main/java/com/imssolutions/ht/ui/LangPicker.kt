package com.imssolutions.ht.ui

import android.graphics.BitmapFactory
import android.util.Base64
import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.DropdownMenu
import androidx.compose.material3.DropdownMenuItem
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.ImageBitmap
import androidx.compose.ui.graphics.asImageBitmap
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.R

private fun decodeIcon(uri: String?): ImageBitmap? {
    val payload = iconPayload(uri) ?: return null
    return runCatching {
        val bytes = Base64.decode(payload, Base64.DEFAULT)
        BitmapFactory.decodeByteArray(bytes, 0, bytes.size)?.asImageBitmap()
    }.getOrNull()
}

private fun builtInFlag(code: String): Int? = when (code.lowercase()) {
    "en" -> R.drawable.flag_en
    "ja" -> R.drawable.flag_ja
    "vi" -> R.drawable.flag_vi
    else -> null
}

@Composable
fun LangIcon(lang: HtLang, width: Int = 24) {
    val height = (width * 0.62f).dp
    val shape = RoundedCornerShape(2.dp)
    val modifier = Modifier.size(width.dp, height).clip(shape).border(0.5.dp, Color(0x33000000), shape)
    val bitmap = remember(lang.icon) { decodeIcon(lang.icon) }
    val fallback = builtInFlag(lang.code)
    when {
        bitmap != null -> Image(bitmap, lang.code, modifier, contentScale = ContentScale.Crop)
        fallback != null -> Image(painterResource(fallback), lang.code, modifier, contentScale = ContentScale.Crop)
        else -> Box(modifier.background(Color(0xFFE2E8F0)), contentAlignment = Alignment.Center) {
            Text(lang.code.uppercase(), fontSize = 8.sp, fontWeight = FontWeight.Bold, color = Color(0xFF4A5568))
        }
    }
}

@Composable
fun LangPicker(
    selected: HtLang,
    onLang: ((HtLang) -> Unit)?,
    modifier: Modifier = Modifier,
) {
    val languages = LocalHtStrings.current.languages
    var open by remember { mutableStateOf(false) }
    val shape = RoundedCornerShape(5.dp)
    Box(modifier) {
        Row(
            modifier = Modifier
                .clip(shape)
                .background(if (open) Color(0xFFEBF8FF) else Color.White)
                .border(1.5.dp, if (open) Color(0xFF007AC9) else Color(0xFFCBD5E0), shape)
                .then(if (onLang != null) Modifier.clickable { open = true } else Modifier)
                .padding(horizontal = 6.dp, vertical = 4.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(5.dp),
        ) {
            LangIcon(selected, width = 22)
            if (onLang != null) Text(if (open) "▲" else "▼", color = Color(0xFF718096), fontSize = 9.sp)
        }
        DropdownMenu(
            expanded = open,
            onDismissRequest = { open = false },
            containerColor = Color.White,
        ) {
            languages.forEach { l ->
                val active = l.code == selected.code
                DropdownMenuItem(
                    modifier = Modifier.background(if (active) Color(0xFF007AC9) else Color.Transparent),
                    leadingIcon = { LangIcon(l, width = 22) },
                    text = {
                        Text(
                            l.name,
                            fontSize = 13.sp,
                            fontWeight = FontWeight.Medium,
                            color = if (active) Color.White else Color(0xFF2D3748),
                        )
                    },
                    onClick = {
                        open = false
                        onLang?.invoke(l)
                    },
                )
            }
        }
    }
}
