package com.imssolutions.ht.ui.screen

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.RowScope
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.vector.ImageVector
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.imssolutions.ht.data.ProductionOrder
import com.imssolutions.ht.ui.ErrorSound
import com.imssolutions.ht.ui.HtItemCard
import com.imssolutions.ht.ui.t
import com.imssolutions.ht.ui.theme.BadgeInfo
import com.imssolutions.ht.ui.theme.BadgeOk
import com.imssolutions.ht.ui.theme.BadgeWarn
import com.imssolutions.ht.ui.vm.fmt

val PrdBlue = Color(0xFF007AC9)
val PrdGreen = Color(0xFF38A169)
val PrdOrange = Color(0xFFDD6B20)
val PrdRed = Color(0xFFE53E3E)

@Composable
fun prdStatusName(code: Int): String = when (code) {
    0 -> t("hpStPending", "Not Started")
    1 -> t("hpStInProgress", "In Progress")
    2 -> t("hpStCompleted", "Completed")
    else -> t("hpStCanceled", "Cancelled")
}

@Composable
fun PrdBadge(text: String, color: Color) {
    Box(
        Modifier
            .clip(RoundedCornerShape(10.dp))
            .background(color)
            .padding(horizontal = 8.dp, vertical = 2.dp),
    ) {
        Text(
            text,
            fontSize = 10.sp,
            fontWeight = FontWeight.SemiBold,
            color = if (color == BadgeWarn) Color(0xFF3D2E00) else Color.White,
        )
    }
}

@Composable
fun PrdOrderCard(order: ProductionOrder, meta: String? = null, extraBadge: (@Composable () -> Unit)? = null, onClick: () -> Unit) {
    HtItemCard(
        code = order.productionOrderNo,
        subtitle = buildString {
            append(order.outputItemCode)
            order.outputItemName?.let { append(" ").append(it) }
            append("  ·  ").append(t("hpPlanned", "Planned")).append(": ")
                .append(fmt(order.plannedQty)).append(' ').append(order.uomCode)
            if (order.plannedStartDate.isNotBlank()) append("  ·  ").append(order.plannedStartDate)
            meta?.let { append("  ·  ").append(it) }
        },
        accent = if (order.statusCode == 1) BadgeOk else Color.Transparent,
        badge = {
            Row(horizontalArrangement = Arrangement.spacedBy(4.dp)) {
                extraBadge?.invoke()
                PrdBadge(prdStatusName(order.statusCode), if (order.statusCode == 1) BadgeWarn else BadgeInfo)
            }
        },
        onClick = onClick,
    )
}

@Composable
fun PrdCard(title: String?, modifier: Modifier = Modifier, accent: Color? = null, content: @Composable ColumnScope.() -> Unit) {
    Row(
        modifier
            .fillMaxWidth()
            .padding(bottom = 10.dp)
            .clip(RoundedCornerShape(10.dp))
            .background(MaterialTheme.colorScheme.surface)
            .height(IntrinsicSize.Min),
    ) {
        if (accent != null) Box(Modifier.width(3.dp).fillMaxHeight().background(accent))
        Column(Modifier.weight(1f).padding(12.dp)) {
            if (title != null) {
                Text(title, fontSize = 11.sp, fontWeight = FontWeight.Bold, color = PrdBlue)
                Spacer(Modifier.height(8.dp))
            }
            content()
        }
    }
}

@Composable
fun PrdRow(label: String, value: String, valueColor: Color = MaterialTheme.colorScheme.onSurface) {
    Row(
        Modifier.fillMaxWidth().padding(vertical = 4.dp),
        horizontalArrangement = Arrangement.SpaceBetween,
        verticalAlignment = Alignment.CenterVertically,
    ) {
        Text(label, fontSize = 12.sp, color = MaterialTheme.colorScheme.onSurfaceVariant, modifier = Modifier.weight(1f))
        Spacer(Modifier.width(8.dp))
        Text(value, fontSize = 12.5.sp, fontWeight = FontWeight.SemiBold, color = valueColor, textAlign = TextAlign.End)
    }
}

@Composable
fun PrdField(label: String, content: @Composable () -> Unit) {
    Column(Modifier.fillMaxWidth().padding(bottom = 10.dp)) {
        Text(
            label,
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold,
            color = MaterialTheme.colorScheme.onSurfaceVariant,
            modifier = Modifier.padding(bottom = 4.dp),
        )
        content()
    }
}

@Composable
fun PrdInfo(text: String, warn: Boolean = false) {
    Text(
        text,
        fontSize = 11.sp,
        color = if (warn) PrdOrange else Color(0xFF718096),
        fontWeight = if (warn) FontWeight.SemiBold else FontWeight.Normal,
        modifier = Modifier.padding(top = 5.dp),
    )
}

@Composable
fun PrdOkNote(text: String) {
    Text(
        text,
        fontSize = 11.5.sp,
        color = Color(0xFF276749),
        modifier = Modifier
            .fillMaxWidth()
            .padding(vertical = 6.dp)
            .clip(RoundedCornerShape(8.dp))
            .background(Color(0xFFE6F6EC))
            .padding(horizontal = 10.dp, vertical = 8.dp),
    )
}

@Composable
fun PrdActionBar(content: @Composable RowScope.() -> Unit) {
    Row(
        Modifier
            .fillMaxWidth()
            .background(MaterialTheme.colorScheme.surface)
            .padding(10.dp),
        horizontalArrangement = Arrangement.spacedBy(8.dp),
        content = content,
    )
}

@Composable
fun RowScope.PrdActionButton(
    text: String,
    color: Color,
    enabled: Boolean = true,
    icon: ImageVector? = Icons.Default.Check,
    onClick: () -> Unit,
) {
    Button(
        onClick = onClick,
        enabled = enabled,
        shape = RoundedCornerShape(8.dp),
        colors = ButtonDefaults.buttonColors(containerColor = color, disabledContainerColor = color.copy(alpha = 0.5f)),
        modifier = Modifier.weight(1f).height(44.dp),
    ) {
        if (icon != null) {
            Icon(icon, null, tint = Color.White)
            Spacer(Modifier.width(6.dp))
        }
        Text(text, fontWeight = FontWeight.Bold, color = Color.White)
    }
}

@Composable
fun PrdErrorDialog(error: Pair<String, String>?, onDismiss: () -> Unit) {
    ErrorSound(error)
    val (code, message) = error ?: return
    AlertDialog(
        onDismissRequest = onDismiss,
        title = { Text(t("hpDlgError", "Error")) },
        text = { Text("$code: ${t(code, message, message)}") },
        confirmButton = { Button(onClick = onDismiss) { Text("OK") } },
    )
}

@Composable
fun PrdInfoDialog(message: String, onOk: () -> Unit, onCancel: (() -> Unit)? = null) {
    AlertDialog(
        onDismissRequest = { onCancel?.invoke() ?: onOk() },
        title = { Text(t("hpDlgInfo", "Information")) },
        text = { Text(message) },
        confirmButton = { Button(onClick = onOk) { Text("OK") } },
        dismissButton = onCancel?.let { { TextButton(onClick = it) { Text(t("htCancel", "Cancel")) } } },
    )
}

@Composable
fun PrdScanMsg(text: String) {
    Text(
        text,
        fontSize = 11.5.sp,
        color = Color(0xFF2A4365),
        modifier = Modifier
            .fillMaxWidth()
            .padding(bottom = 8.dp)
            .clip(RoundedCornerShape(8.dp))
            .background(Color(0xFFEEF6FC))
            .padding(horizontal = 10.dp, vertical = 8.dp),
    )
}
