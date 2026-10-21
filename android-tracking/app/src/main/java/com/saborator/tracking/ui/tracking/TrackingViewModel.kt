package com.saborator.tracking.ui.tracking

import androidx.lifecycle.ViewModel
import androidx.lifecycle.viewModelScope
import com.saborator.tracking.data.MockTrackingData
import com.saborator.tracking.data.model.EstadoPedido
import com.saborator.tracking.data.model.OrderTrackingState
import kotlinx.coroutines.Job
import kotlinx.coroutines.delay
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.update
import kotlinx.coroutines.isActive
import kotlinx.coroutines.launch
import kotlin.math.ceil
import kotlin.math.max

/** Maneja el estado del pedido y simula el recorrido del repartidor sin backend. */
class TrackingViewModel : ViewModel() {

    private val route = MockTrackingData.route
    private val lastIndex = route.lastIndex

    private val _state = MutableStateFlow(buildState(EstadoPedido.ON_THE_WAY, riderIndex = 0))
    val state: StateFlow<OrderTrackingState> = _state.asStateFlow()

    private var movementJob: Job? = null

    init {
        // La demo arranca con el repartidor ya en camino
        startMovement()
    }

    /** Avanza al siguiente estado; si el pedido ya fue entregado reinicia la demo. */
    fun simulateNextState() {
        val next = _state.value.status.next()
        if (next == null) {
            resetDemo()
            return
        }
        movementJob?.cancel()
        when (next) {
            EstadoPedido.ON_THE_WAY -> {
                _state.value = buildState(next, riderIndex = 0)
                startMovement()
            }
            EstadoPedido.DELIVERED -> _state.value = buildState(next, riderIndex = lastIndex)
            else -> _state.value = buildState(next, riderIndex = 0)
        }
    }

    private fun resetDemo() {
        movementJob?.cancel()
        _state.value = buildState(EstadoPedido.CONFIRMED, riderIndex = 0)
    }

    /** Cada 2 segundos mueve al repartidor al siguiente punto de la ruta. */
    private fun startMovement() {
        movementJob?.cancel()
        movementJob = viewModelScope.launch {
            while (isActive && _state.value.riderIndex < lastIndex) {
                delay(STEP_DELAY_MS)
                _state.update { current ->
                    val newIndex = current.riderIndex + 1
                    // Al llegar al último punto el pedido se marca como entregado
                    val status = if (newIndex >= lastIndex) EstadoPedido.DELIVERED else current.status
                    buildState(status, newIndex)
                }
            }
        }
    }

    private fun buildState(status: EstadoPedido, riderIndex: Int): OrderTrackingState {
        val remaining = MockTrackingData.remainingMeters(route, riderIndex)
        return OrderTrackingState(
            status = status,
            rider = MockTrackingData.rider,
            route = route,
            riderIndex = riderIndex,
            remainingMeters = remaining,
            etaMinutes = max(1, ceil(remaining / 1000.0 * MINUTES_PER_KM).toInt()),
            stepTimes = MockTrackingData.stepTimes
        )
    }

    private companion object {
        const val STEP_DELAY_MS = 2_000L

        /** Velocidad urbana simulada (~9 km/h con tráfico y semáforos). */
        const val MINUTES_PER_KM = 6.5
    }
}
