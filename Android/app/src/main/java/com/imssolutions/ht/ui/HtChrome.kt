package com.imssolutions.ht.ui

import androidx.compose.foundation.Image
import androidx.compose.foundation.background
import androidx.compose.foundation.border
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.automirrored.filled.ArrowBack
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import kotlinx.coroutines.delay
import java.util.Calendar
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.layout.ContentScale
import androidx.compose.ui.res.painterResource
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.R
import com.imssolutions.ht.ui.theme.BrandBlue
import com.imssolutions.ht.ui.theme.BrandBlueDark


enum class HtLang(val code: String, val flag: Int) {
    EN("en", R.drawable.flag_en),
    JA("ja", R.drawable.flag_ja),
    VI("vi", R.drawable.flag_vi);

    companion object {
        fun of(code: String?): HtLang =
            entries.firstOrNull { it.code.equals(code?.trim(), ignoreCase = true) } ?: VI
    }
}

@Composable
fun HtHeader(
    title: String,
    onBack: (() -> Unit)? = null,
    lang: HtLang = HtLang.VI,
    onLang: ((HtLang) -> Unit)? = null,
) {
    Column(Modifier.fillMaxWidth().background(BrandBlue).statusBarsPadding()) {
    Row(
        Modifier
            .fillMaxWidth()
            .height(46.dp)
            .padding(horizontal = 10.dp),
        verticalAlignment = Alignment.CenterVertically,
    ) {
        if (onBack != null) {
            Icon(
                Icons.AutoMirrored.Filled.ArrowBack,
                contentDescription = "Quay lại",
                tint = Color.White,
                modifier = Modifier
                    .clip(RoundedCornerShape(6.dp))
                    .clickable(onClick = onBack)
                    .padding(5.dp),
            )
            Spacer(Modifier.width(6.dp))
        }
        Text(
            title,
            color = Color.White,
            fontWeight = FontWeight.Bold,
            fontSize = 14.sp,
            maxLines = 1,
            modifier = Modifier.weight(1f),
        )
        LangFlags(lang, onLang)
    }
    }
}

@Composable
fun LangFlags(selected: HtLang, onLang: ((HtLang) -> Unit)?, size: Int = 22) {
    Row(horizontalArrangement = Arrangement.spacedBy(3.dp)) {
        HtLang.entries.forEach { l ->
            Image(
                painter = painterResource(l.flag),
                contentDescription = l.code,
                contentScale = ContentScale.Fit,
                modifier = Modifier
                    .size(size.dp, (size * 0.62).dp)
                    .alpha(if (l == selected) 1f else 0.45f)
                    .border(
                        1.dp,
                        if (l == selected) Color.White else Color.Transparent,
                        RoundedCornerShape(2.dp),
                    )
                    .then(if (onLang != null) Modifier.clickable { onLang(l) } else Modifier),
            )
        }
    }
}

@Composable
fun HtStatusBar(clock: String, right: String) {
    Row(
        Modifier
            .fillMaxWidth()
            .background(BrandBlueDark)
            .navigationBarsPadding()
            .height(26.dp)
            .padding(horizontal = 14.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.SpaceBetween,
    ) {
        Text("IMS", color = Color.White, fontSize = 10.sp, fontWeight = FontWeight.Bold)
        Text(clock, color = Color.White, fontSize = 10.sp)
        Text(right, color = Color.White.copy(alpha = 0.85f), fontSize = 10.sp, maxLines = 1)
    }
}

@Composable
fun HtItemCard(
    code: String,
    subtitle: String,
    accent: Color,
    badge: @Composable () -> Unit,
    onClick: () -> Unit,
) {
    Row(
        Modifier
            .fillMaxWidth()
            .padding(bottom = 7.dp)
            .clip(RoundedCornerShape(10.dp))
            .background(MaterialTheme.colorScheme.surface)
            .clickable(onClick = onClick)
            .height(IntrinsicSize.Min),
    ) {
        Box(Modifier.width(4.dp).fillMaxHeight().background(accent))
        Column(Modifier.weight(1f).padding(horizontal = 12.dp, vertical = 11.dp)) {
            Row(
                Modifier.fillMaxWidth(),
                horizontalArrangement = Arrangement.SpaceBetween,
                verticalAlignment = Alignment.CenterVertically,
            ) {
                Text(
                    code,
                    fontSize = 12.5.sp,
                    fontWeight = FontWeight.Bold,
                    color = MaterialTheme.colorScheme.onSurface,
                )
                badge()
            }
            Spacer(Modifier.height(3.dp))
            Text(
                subtitle,
                fontSize = 12.sp,
                color = MaterialTheme.colorScheme.onSurfaceVariant,
                maxLines = 1,
            )
        }
    }
}

val HtBodyBg = Color(0xFFE8ECF1)

@Composable
fun nowClock(): String {
    var text by remember { mutableStateOf(clockText()) }
    LaunchedEffect(Unit) {
        while (true) {
            val now = System.currentTimeMillis()
            delay(60_000 - (now % 60_000))
            text = clockText()
        }
    }
    return text
}

private fun clockText(): String {
    val c = Calendar.getInstance()
    return "%02d:%02d".format(c.get(Calendar.HOUR_OF_DAY), c.get(Calendar.MINUTE))
}
