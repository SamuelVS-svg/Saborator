package com.saborator.tracking.ui.tracking.components

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Phone
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Brush
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.saborator.tracking.data.model.RiderInfo
import com.saborator.tracking.ui.theme.TrackingColors
import java.util.Locale

/** Tarjeta con la foto, nombre, calificación y botón de llamada del repartidor. */
@Composable
fun RiderInfoCard(
    rider: RiderInfo,
    onCallClick: () -> Unit,
    modifier: Modifier = Modifier
) {
    Row(
        modifier = modifier
            .fillMaxWidth()
            .clip(RoundedCornerShape(16.dp))
            .background(TrackingColors.Background)
            .padding(10.dp),
        verticalAlignment = Alignment.CenterVertically,
        horizontalArrangement = Arrangement.spacedBy(12.dp)
    ) {
        RiderAvatar(name = rider.name)

        Column(modifier = Modifier.weight(1f)) {
            Text(
                text = "${rider.name} - ${String.format(Locale.US, "%.1f", rider.rating)} ★",
                fontSize = 15.sp,
                fontWeight = FontWeight.Bold,
                color = TrackingColors.Ink
            )
            Text(text = "Tu repartidor", fontSize = 12.sp, color = TrackingColors.Muted)
        }

        Row(
            modifier = Modifier
                .clip(CircleShape)
                .background(TrackingColors.Orange)
                .clickable(onClick = onCallClick)
                .padding(horizontal = 14.dp, vertical = 9.dp),
            verticalAlignment = Alignment.CenterVertically,
            horizontalArrangement = Arrangement.spacedBy(6.dp)
        ) {
            Icon(
                imageVector = Icons.Filled.Phone,
                contentDescription = null,
                tint = Color.White,
                modifier = Modifier.size(16.dp)
            )
            Text(text = "Llamar", fontSize = 13.sp, fontWeight = FontWeight.Bold, color = Color.White)
        }
    }
}

/**
 * Foto del repartidor. Como no hay backend se dibuja un avatar con la inicial.
 * TODO: reemplazar por la foto real con Coil (AsyncImage) cuando exista la URL.
 */
@Composable
private fun RiderAvatar(name: String) {
    Box(
        modifier = Modifier
            .size(46.dp)
            .clip(CircleShape)
            .background(Brush.linearGradient(listOf(TrackingColors.Orange, Color(0xFFFFA24D)))),
        contentAlignment = Alignment.Center
    ) {
        Text(
            text = name.take(1).uppercase(),
            fontSize = 20.sp,
            fontWeight = FontWeight.ExtraBold,
            color = Color.White
        )
    }
}
