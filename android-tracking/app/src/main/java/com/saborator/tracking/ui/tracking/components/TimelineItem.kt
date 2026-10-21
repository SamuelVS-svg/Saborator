package com.saborator.tracking.ui.tracking.components

import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.RepeatMode
import androidx.compose.animation.core.animateFloat
import androidx.compose.animation.core.infiniteRepeatable
import androidx.compose.animation.core.rememberInfiniteTransition
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.ColumnScope
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Check
import androidx.compose.material3.Icon
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.alpha
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.scale
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.saborator.tracking.data.model.StepStatus
import com.saborator.tracking.ui.theme.TrackingColors
import kotlinx.coroutines.delay

/**
 * Un paso del timeline vertical.
 *
 * @param title texto principal del paso.
 * @param status aspecto del paso: completado (verde), activo (pulso) o pendiente (gris).
 * @param isLast si es el último paso no se dibuja la línea conectora hacia abajo.
 * @param time hora en que ocurrió el paso; null mientras está pendiente.
 * @param showDots muestra la animación de puntos suspensivos junto al título.
 * @param content contenido extra opcional debajo del título (por ejemplo, la tarjeta del repartidor).
 */
@Composable
fun TimelineItem(
    title: String,
    status: StepStatus,
    isLast: Boolean,
    modifier: Modifier = Modifier,
    time: String? = null,
    showDots: Boolean = false,
    content: (@Composable ColumnScope.() -> Unit)? = null
) {
    Row(modifier = modifier.height(IntrinsicSize.Min)) {
        // Columna izquierda: nodo + línea conectora
        Column(
            modifier = Modifier.width(32.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            TimelineNode(status)
            if (!isLast) {
                Box(
                    modifier = Modifier
                        .padding(vertical = 4.dp)
                        .width(3.dp)
                        .weight(1f)
                        .clip(CircleShape)
                        .background(if (status == StepStatus.COMPLETED) TrackingColors.Green else TrackingColors.Gray)
                )
            }
        }

        // Columna derecha: título, hora y contenido opcional
        Column(
            modifier = Modifier
                .weight(1f)
                .padding(start = 12.dp, bottom = if (isLast) 0.dp else 18.dp)
        ) {
            Row(
                modifier = Modifier.fillMaxWidth(),
                verticalAlignment = Alignment.CenterVertically,
                horizontalArrangement = Arrangement.SpaceBetween
            ) {
                Row(modifier = Modifier.weight(1f), verticalAlignment = Alignment.CenterVertically) {
                    Text(
                        text = title,
                        fontSize = 15.sp,
                        fontWeight = if (status == StepStatus.PENDING) FontWeight.Medium else FontWeight.Bold,
                        color = if (status == StepStatus.PENDING) TrackingColors.Muted else TrackingColors.Ink
                    )
                    if (showDots) AnimatedDots()
                }
                if (time != null) {
                    Text(text = time, fontSize = 13.sp, color = TrackingColors.Muted)
                }
            }
            content?.let {
                Column(modifier = Modifier.padding(top = 8.dp), content = it)
            }
        }
    }
}

/** Círculo del paso: verde con check, naranja con pulso o gris. */
@Composable
private fun TimelineNode(status: StepStatus) {
    Box(modifier = Modifier.size(26.dp), contentAlignment = Alignment.Center) {
        when (status) {
            StepStatus.COMPLETED -> Box(
                modifier = Modifier
                    .size(22.dp)
                    .clip(CircleShape)
                    .background(TrackingColors.Green),
                contentAlignment = Alignment.Center
            ) {
                Icon(
                    imageVector = Icons.Filled.Check,
                    contentDescription = null,
                    tint = Color.White,
                    modifier = Modifier.size(14.dp)
                )
            }

            StepStatus.ACTIVE -> {
                val transition = rememberInfiniteTransition(label = "pulse")
                val pulseScale by transition.animateFloat(
                    initialValue = 0.7f,
                    targetValue = 1.5f,
                    animationSpec = infiniteRepeatable(tween(1200, easing = LinearEasing), RepeatMode.Restart),
                    label = "pulseScale"
                )
                val pulseAlpha by transition.animateFloat(
                    initialValue = 0.55f,
                    targetValue = 0f,
                    animationSpec = infiniteRepeatable(tween(1200, easing = LinearEasing), RepeatMode.Restart),
                    label = "pulseAlpha"
                )
                // Halo que se expande y se desvanece
                Box(
                    modifier = Modifier
                        .size(26.dp)
                        .scale(pulseScale)
                        .alpha(pulseAlpha)
                        .clip(CircleShape)
                        .background(TrackingColors.Orange)
                )
                Box(
                    modifier = Modifier
                        .size(20.dp)
                        .clip(CircleShape)
                        .background(TrackingColors.Orange),
                    contentAlignment = Alignment.Center
                ) {
                    Box(
                        modifier = Modifier
                            .size(8.dp)
                            .clip(CircleShape)
                            .background(Color.White)
                    )
                }
            }

            StepStatus.PENDING -> Box(
                modifier = Modifier
                    .size(18.dp)
                    .clip(CircleShape)
                    .background(TrackingColors.Gray)
            )
        }
    }
}

/** Puntos suspensivos animados ("." -> ".." -> "..."). */
@Composable
private fun AnimatedDots() {
    var count by remember { mutableIntStateOf(0) }
    LaunchedEffect(Unit) {
        while (true) {
            delay(400)
            count = (count + 1) % 4
        }
    }
    Text(
        text = ".".repeat(count),
        fontSize = 15.sp,
        fontWeight = FontWeight.Bold,
        color = TrackingColors.Orange,
        modifier = Modifier.width(18.dp)
    )
}
