package com.saborator.tracking.ui.theme

import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.lightColorScheme
import androidx.compose.runtime.Composable
import androidx.compose.ui.graphics.Color

/** Paleta propia de la app (naranja de marca, verde de completado y grises neutros). */
object TrackingColors {
    val Orange = Color(0xFFFF6A00)
    val OrangeSoft = Color(0xFFFFEBDD)
    val Green = Color(0xFF22B573)
    val Gray = Color(0xFFD5D8DE)
    val Ink = Color(0xFF1F2430)
    val Muted = Color(0xFF8A90A0)
    val Background = Color(0xFFF6F7F9)
    val Surface = Color.White
}

@Composable
fun TrackingTheme(content: @Composable () -> Unit) {
    MaterialTheme(
        colorScheme = lightColorScheme(
            primary = TrackingColors.Orange,
            onPrimary = Color.White,
            background = TrackingColors.Background,
            surface = TrackingColors.Surface,
            onSurface = TrackingColors.Ink
        ),
        content = content
    )
}
