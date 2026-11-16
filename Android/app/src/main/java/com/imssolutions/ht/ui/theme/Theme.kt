package com.imssolutions.ht.ui.theme

import com.imssolutions.ht.ui.t

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Typography
import androidx.compose.material3.darkColorScheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.runtime.ReadOnlyComposable
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.sp

val BrandBlue = Color(0xFF007AC9)
val BrandBlueDark = Color(0xFF005FA3)
val BrandBlueLight = Color(0xFFE3F2FB)

val BadgeOk = Color(0xFF28A745)
val BadgeWarn = Color(0xFFFFC107)
val BadgeNg = Color(0xFFDC3545)
val BadgeInfo = Color(0xFF17A2B8)
val BadgeCancel = Color(0xFF6C757D)

val ScreenBg = Color(0xFFF2F5F8)
val CardBg = Color(0xFFFFFFFF)
val TextMain = Color(0xFF1F2937)
val TextSub = Color(0xFF6B7280)

private val LightColors = lightColorScheme(
    primary = BrandBlue,
    onPrimary = Color.White,
    primaryContainer = BrandBlueLight,
    onPrimaryContainer = BrandBlueDark,
    secondary = BrandBlueDark,
    onSecondary = Color.White,
    background = ScreenBg,
    onBackground = TextMain,
    surface = CardBg,
    onSurface = TextMain,
    surfaceVariant = Color(0xFFE8EDF2),
    onSurfaceVariant = TextSub,
    error = BadgeNg,
    onError = Color.White,
    outline = Color(0xFFD8DCE2),
)

private val DarkColors = darkColorScheme(
    primary = Color(0xFF4FB3E8),
    onPrimary = Color(0xFF00344F),
    background = Color(0xFF11161B),
    onBackground = Color(0xFFE6EAEE),
    surface = Color(0xFF1A2027),
    onSurface = Color(0xFFE6EAEE),
    surfaceVariant = Color(0xFF262E37),
    onSurfaceVariant = Color(0xFFA9B2BC),
    error = Color(0xFFFF6B7A),
    outline = Color(0xFF3A434E),
)

private val HtTypography = Typography(
    titleLarge = TextStyle(fontSize = 20.sp, fontWeight = FontWeight.Bold),
    titleMedium = TextStyle(fontSize = 17.sp, fontWeight = FontWeight.SemiBold),
    bodyLarge = TextStyle(fontSize = 16.sp),
    bodyMedium = TextStyle(fontSize = 14.sp),
    bodySmall = TextStyle(fontSize = 12.sp),
    labelLarge = TextStyle(fontSize = 15.sp, fontWeight = FontWeight.SemiBold),
    labelSmall = TextStyle(fontSize = 11.sp, fontWeight = FontWeight.Medium),
)

@Composable
fun ImsTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit,
) {
    MaterialTheme(
        colorScheme = if (darkTheme) DarkColors else LightColors,
        typography = HtTypography,
        content = content,
    )
}

fun statusColor(status: String): Color = when (status) {
    "statusArrived", "statusReturned", "statusShipped", "statusPicked" -> BadgeOk
    "statusPartial", "statusPartialReturn", "statusPartialShip" -> BadgeWarn
    "statusCancelled" -> BadgeCancel
    else -> BadgeInfo
}

@Composable
@ReadOnlyComposable
fun statusLabel(status: String): String = t(status, statusFallback(status))

private fun statusFallback(status: String): String = when (status) {
    "statusArrived" -> "Arrived"
    "statusPartial" -> "Partial"
    "statusReturned" -> "Returned"
    "statusPartialReturn" -> "Partially returned"
    "statusCancelled" -> "Cancelled"
    "statusPendingReturn" -> "Pending"
    "statusShipped" -> "Shipped"
    "statusPartialShip" -> "Partial"
    "statusNotShipped" -> "Not shipped"
    "statusPicked" -> "Picked"
    "statusNotPicked" -> "Not picked"
    else -> "Not arrived"
}
