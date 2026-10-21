package com.saborator.tracking.data.model

import com.google.android.gms.maps.model.LatLng

/** Estados del pedido, en orden cronológico. */
enum class EstadoPedido(val title: String) {
    CONFIRMED("Pedido confirmado"),
    PREPARING("Preparando tu pedido"),
    RIDER_ASSIGNED("Repartidor asignado"),
    ON_THE_WAY("Repartidor en camino"),
    DELIVERED("Entregado");

    /** Siguiente estado, o null si el pedido ya fue entregado. */
    fun next(): EstadoPedido? = entries.getOrNull(ordinal + 1)
}

/** Aspecto visual de un paso dentro del timeline. */
enum class StepStatus { COMPLETED, ACTIVE, PENDING }

/** Datos del repartidor que se muestran en la tarjeta. */
data class RiderInfo(
    val name: String,
    val rating: Double,
    val phone: String
)

/**
 * Estado completo de la pantalla de seguimiento.
 *
 * La posición del repartidor se expresa como índice dentro de [route]; la interfaz
 * se encarga de animar el movimiento suave entre un índice y el siguiente.
 */
data class OrderTrackingState(
    val status: EstadoPedido,
    val rider: RiderInfo,
    val route: List<LatLng>,
    val riderIndex: Int,
    val remainingMeters: Double,
    val etaMinutes: Int,
    val stepTimes: Map<EstadoPedido, String>
) {
    val restaurant: LatLng get() = route.first()
    val destination: LatLng get() = route.last()
    val remainingKm: Double get() = remainingMeters / 1000.0
}
