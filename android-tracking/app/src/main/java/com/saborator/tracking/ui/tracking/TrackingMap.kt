package com.saborator.tracking.ui.tracking

import android.content.Context
import android.graphics.Bitmap
import android.graphics.Canvas
import androidx.annotation.DrawableRes
import androidx.compose.animation.core.Animatable
import androidx.compose.animation.core.LinearEasing
import androidx.compose.animation.core.tween
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableIntStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.platform.LocalContext
import androidx.core.content.ContextCompat
import com.google.android.gms.maps.CameraUpdateFactory
import com.google.android.gms.maps.model.BitmapDescriptor
import com.google.android.gms.maps.model.BitmapDescriptorFactory
import com.google.android.gms.maps.model.JointType
import com.google.android.gms.maps.model.LatLng
import com.google.android.gms.maps.model.LatLngBounds
import com.google.maps.android.compose.GoogleMap
import com.google.maps.android.compose.MapUiSettings
import com.google.maps.android.compose.Marker
import com.google.maps.android.compose.Polyline
import com.google.maps.android.compose.rememberCameraPositionState
import com.google.maps.android.compose.rememberUpdatedMarkerState
import com.saborator.tracking.R
import com.saborator.tracking.data.model.OrderTrackingState
import com.saborator.tracking.ui.theme.TrackingColors
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.sin

/** Duración de la animación entre dos puntos; un poco menor al intervalo de 2 s para no quedar "pegada". */
private const val MOVE_ANIMATION_MS = 1_900

/**
 * Mapa en vivo con restaurante, repartidor y destino, más la ruta dibujada.
 * El repartidor se desliza suavemente entre los puntos de [OrderTrackingState.route].
 */
@Composable
fun TrackingMap(
    state: OrderTrackingState,
    modifier: Modifier = Modifier
) {
    val context = LocalContext.current
    val cameraPositionState = rememberCameraPositionState()
    val route = state.route

    val rider = rememberAnimatedRider(route = route, riderIndex = state.riderIndex)
    val riderMarkerState = rememberUpdatedMarkerState(position = rider.position)

    GoogleMap(
        modifier = modifier,
        cameraPositionState = cameraPositionState,
        uiSettings = MapUiSettings(zoomControlsEnabled = false, mapToolbarEnabled = false),
        onMapLoaded = {
            // Encuadra toda la ruta con un margen en píxeles
            val bounds = LatLngBounds.builder().apply { route.forEach(::include) }.build()
            cameraPositionState.move(CameraUpdateFactory.newLatLngBounds(bounds, 120))
        }
    ) {
        // Los iconos se crean aquí porque BitmapDescriptorFactory exige que el mapa ya esté inicializado
        val storeIcon = remember { vectorToDescriptor(context, R.drawable.ic_marker_store) }
        val homeIcon = remember { vectorToDescriptor(context, R.drawable.ic_marker_home) }
        val riderIcon = remember { vectorToDescriptor(context, R.drawable.ic_marker_rider) }

        // Ruta completa
        Polyline(
            points = route,
            color = TrackingColors.Orange,
            width = 14f,
            jointType = JointType.ROUND
        )

        Marker(
            state = rememberUpdatedMarkerState(position = state.restaurant),
            icon = storeIcon,
            anchor = Offset(0.5f, 0.5f),
            title = "Restaurante"
        )
        Marker(
            state = rememberUpdatedMarkerState(position = state.destination),
            icon = homeIcon,
            anchor = Offset(0.5f, 0.5f),
            title = "Tu casa"
        )
        Marker(
            state = riderMarkerState,
            icon = riderIcon,
            anchor = Offset(0.5f, 0.5f),
            flat = true,
            rotation = rider.bearing,
            zIndex = 2f,
            title = "Repartidor"
        )
    }
}

/** Posición interpolada del repartidor y hacia dónde apunta. */
private data class AnimatedRider(val position: LatLng, val bearing: Float)

/**
 * Anima la posición del repartidor entre el punto anterior y el actual de la ruta.
 * Si el índice salta o retrocede (reinicio de la demo) se coloca directamente sin animar.
 */
@Composable
private fun rememberAnimatedRider(route: List<LatLng>, riderIndex: Int): AnimatedRider {
    var segmentStart by remember { mutableIntStateOf(riderIndex) }
    var segmentEnd by remember { mutableIntStateOf(riderIndex) }
    val progress = remember { Animatable(1f) }

    LaunchedEffect(riderIndex) {
        if (riderIndex == segmentEnd) return@LaunchedEffect
        if (riderIndex - segmentEnd == 1) {
            segmentStart = segmentEnd
            segmentEnd = riderIndex
            progress.snapTo(0f)
            progress.animateTo(1f, tween(MOVE_ANIMATION_MS, easing = LinearEasing))
        } else {
            segmentStart = riderIndex
            segmentEnd = riderIndex
            progress.snapTo(1f)
        }
    }

    val from = route[segmentStart]
    val to = route[segmentEnd]
    val t = progress.value.toDouble()
    val position = LatLng(
        from.latitude + (to.latitude - from.latitude) * t,
        from.longitude + (to.longitude - from.longitude) * t
    )
    return AnimatedRider(position, bearingAt(route, segmentEnd))
}

/** Rumbo (0° = norte) del tramo que termina en [index]; para el primer punto usa el tramo siguiente. */
private fun bearingAt(route: List<LatLng>, index: Int): Float {
    val from = if (index > 0) route[index - 1] else route[0]
    val to = if (index > 0) route[index] else route[1]
    val lat1 = Math.toRadians(from.latitude)
    val lat2 = Math.toRadians(to.latitude)
    val dLng = Math.toRadians(to.longitude - from.longitude)
    val y = sin(dLng) * cos(lat2)
    val x = cos(lat1) * sin(lat2) - sin(lat1) * cos(lat2) * cos(dLng)
    return ((Math.toDegrees(atan2(y, x)) + 360) % 360).toFloat()
}

/** Convierte un vector drawable en un icono de marker. */
private fun vectorToDescriptor(context: Context, @DrawableRes resId: Int): BitmapDescriptor {
    val drawable = checkNotNull(ContextCompat.getDrawable(context, resId)) { "Drawable $resId no encontrado" }
    val bitmap = Bitmap.createBitmap(drawable.intrinsicWidth, drawable.intrinsicHeight, Bitmap.Config.ARGB_8888)
    val canvas = Canvas(bitmap)
    drawable.setBounds(0, 0, canvas.width, canvas.height)
    drawable.draw(canvas)
    return BitmapDescriptorFactory.fromBitmap(bitmap)
}
