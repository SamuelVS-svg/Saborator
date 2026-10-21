package com.saborator.tracking.data

import com.google.android.gms.maps.model.LatLng
import com.saborator.tracking.data.model.EstadoPedido
import com.saborator.tracking.data.model.RiderInfo
import kotlin.math.atan2
import kotlin.math.cos
import kotlin.math.pow
import kotlin.math.sin
import kotlin.math.sqrt

/** Datos simulados mientras no exista un backend real. */
object MockTrackingData {

    val rider = RiderInfo(name = "Carlos", rating = 4.9, phone = "+573001234567")

    /** Ruta de ~1.2 km (restaurante -> casa del cliente) siguiendo calles en ángulo recto. */
    val route: List<LatLng> = listOf(
        LatLng(4.67600, -74.04800),
        LatLng(4.67600, -74.04950),
        LatLng(4.67520, -74.04950),
        LatLng(4.67520, -74.05080),
        LatLng(4.67430, -74.05080),
        LatLng(4.67430, -74.05200),
        LatLng(4.67350, -74.05200),
        LatLng(4.67350, -74.05320),
        LatLng(4.67270, -74.05320),
        LatLng(4.67270, -74.05430),
        LatLng(4.67190, -74.05430),
        LatLng(4.67190, -74.05520)
    )

    /** Hora en que se alcanzó cada estado (texto fijo para la demo). */
    val stepTimes: Map<EstadoPedido, String> = mapOf(
        EstadoPedido.CONFIRMED to "19:30",
        EstadoPedido.PREPARING to "19:35",
        EstadoPedido.RIDER_ASSIGNED to "19:48",
        EstadoPedido.ON_THE_WAY to "19:52",
        EstadoPedido.DELIVERED to "20:04"
    )

    private const val EARTH_RADIUS_METERS = 6_371_000.0

    /** Distancia entre dos coordenadas con la fórmula de Haversine, en metros. */
    fun distanceMeters(from: LatLng, to: LatLng): Double {
        val dLat = Math.toRadians(to.latitude - from.latitude)
        val dLng = Math.toRadians(to.longitude - from.longitude)
        val a = sin(dLat / 2).pow(2) +
            cos(Math.toRadians(from.latitude)) * cos(Math.toRadians(to.latitude)) * sin(dLng / 2).pow(2)
        return 2 * EARTH_RADIUS_METERS * atan2(sqrt(a), sqrt(1 - a))
    }

    /** Metros que faltan desde el punto [index] hasta el final de [points]. */
    fun remainingMeters(points: List<LatLng>, index: Int): Double =
        (index until points.lastIndex).sumOf { distanceMeters(points[it], points[it + 1]) }
}
