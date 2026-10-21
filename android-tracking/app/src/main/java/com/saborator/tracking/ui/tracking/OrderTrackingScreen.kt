package com.saborator.tracking.ui.tracking

import android.content.Intent
import android.net.Uri
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.navigationBarsPadding
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.statusBarsPadding
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.shadow
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import androidx.lifecycle.compose.collectAsStateWithLifecycle
import androidx.lifecycle.viewmodel.compose.viewModel
import com.saborator.tracking.data.model.EstadoPedido
import com.saborator.tracking.data.model.OrderTrackingState
import com.saborator.tracking.data.model.StepStatus
import com.saborator.tracking.ui.theme.TrackingColors
import com.saborator.tracking.ui.tracking.components.RiderInfoCard
import com.saborator.tracking.ui.tracking.components.TimelineItem
import java.util.Locale

/** Pantalla de seguimiento: timeline de estados arriba y mapa en vivo abajo. */
@Composable
fun OrderTrackingScreen(viewModel: TrackingViewModel = viewModel()) {
    val state by viewModel.state.collectAsStateWithLifecycle()
    OrderTrackingContent(state = state, onSimulateNext = viewModel::simulateNextState)
}

@Composable
private fun OrderTrackingContent(
    state: OrderTrackingState,
    onSimulateNext: () -> Unit
) {
    val context = LocalContext.current

    Column(
        modifier = Modifier
            .fillMaxSize()
            .background(TrackingColors.Background)
    ) {
        // ── Sección 1: encabezado + timeline ──────────────────────────────
        Column(
            modifier = Modifier
                .weight(1f)
                .statusBarsPadding()
        ) {
            Text(
                text = "Sigue tu pedido",
                fontSize = 22.sp,
                fontWeight = FontWeight.ExtraBold,
                color = TrackingColors.Ink,
                modifier = Modifier.padding(start = 20.dp, top = 12.dp, end = 20.dp, bottom = 8.dp)
            )

            Column(
                modifier = Modifier
                    .weight(1f)
                    .padding(horizontal = 16.dp)
                    .clip(RoundedCornerShape(20.dp))
                    .background(TrackingColors.Surface)
                    .verticalScroll(rememberScrollState())
                    .padding(horizontal = 18.dp, vertical = 18.dp)
            ) {
                EstadoPedido.entries.forEachIndexed { index, step ->
                    val stepStatus = stepStatusFor(step, state.status)
                    TimelineItem(
                        title = step.title,
                        status = stepStatus,
                        isLast = index == EstadoPedido.entries.lastIndex,
                        time = state.stepTimes[step].takeIf { stepStatus != StepStatus.PENDING },
                        showDots = step == EstadoPedido.PREPARING && stepStatus == StepStatus.ACTIVE,
                        // La tarjeta del repartidor aparece junto al paso "Repartidor asignado"
                        content = if (step == EstadoPedido.RIDER_ASSIGNED && stepStatus != StepStatus.PENDING) {
                            {
                                RiderInfoCard(
                                    rider = state.rider,
                                    onCallClick = {
                                        context.startActivity(
                                            Intent(Intent.ACTION_DIAL, Uri.parse("tel:${state.rider.phone}"))
                                        )
                                    }
                                )
                            }
                        } else {
                            null
                        }
                    )
                }
            }

            DemoButton(
                label = if (state.status.next() == null) "Reiniciar demo" else "Simular siguiente estado",
                onClick = onSimulateNext
            )
        }

        // ── Sección 2: mapa en vivo con tarjeta flotante ──────────────────
        Box(
            modifier = Modifier
                .weight(1f)
                .fillMaxWidth()
                .clip(RoundedCornerShape(topStart = 24.dp, topEnd = 24.dp))
        ) {
            TrackingMap(state = state, modifier = Modifier.fillMaxSize())
            EtaSheet(
                state = state,
                modifier = Modifier
                    .align(Alignment.BottomCenter)
                    .navigationBarsPadding()
                    .padding(16.dp)
            )
        }
    }
}

/** Define cómo se pinta [step] según el estado actual del pedido. */
private fun stepStatusFor(step: EstadoPedido, current: EstadoPedido): StepStatus = when {
    current == EstadoPedido.DELIVERED || step.ordinal < current.ordinal -> StepStatus.COMPLETED
    step == current -> StepStatus.ACTIVE
    else -> StepStatus.PENDING
}

/** Botón para avanzar el estado durante la demo en clase. */
@Composable
private fun DemoButton(label: String, onClick: () -> Unit) {
    Box(
        modifier = Modifier
            .fillMaxWidth()
            .padding(horizontal = 16.dp, vertical = 10.dp)
            .clip(CircleShape)
            .background(TrackingColors.Ink)
            .clickable(onClick = onClick)
            .padding(vertical = 13.dp),
        contentAlignment = Alignment.Center
    ) {
        Text(text = label, color = Color.White, fontSize = 14.sp, fontWeight = FontWeight.Bold)
    }
}

/** Tarjeta flotante sobre el mapa con el tiempo y la distancia restantes. */
@Composable
private fun EtaSheet(state: OrderTrackingState, modifier: Modifier = Modifier) {
    val (headline, detail) = when {
        state.status == EstadoPedido.DELIVERED -> "¡Tu pedido llegó!" to "Disfruta tu comida"
        state.status == EstadoPedido.ON_THE_WAY -> {
            val km = String.format(Locale.US, "%.1f", state.remainingKm)
            "Llega en ${state.etaMinutes} min - $km km" to "Carlos va hacia tu dirección"
        }
        else -> "Preparando tu pedido" to "Te avisaremos cuando salga el repartidor"
    }

    Column(
        modifier = modifier
            .fillMaxWidth()
            .shadow(12.dp, RoundedCornerShape(22.dp))
            .clip(RoundedCornerShape(22.dp))
            .background(TrackingColors.Surface)
            .padding(horizontal = 20.dp, vertical = 12.dp),
        horizontalAlignment = Alignment.CenterHorizontally
    ) {
        // Asa visual del bottom sheet
        Box(
            modifier = Modifier
                .width(36.dp)
                .height(4.dp)
                .clip(CircleShape)
                .background(TrackingColors.Gray)
        )
        Row(
            modifier = Modifier
                .fillMaxWidth()
                .padding(top = 10.dp),
            horizontalArrangement = Arrangement.Center
        ) {
            Text(
                text = headline,
                fontSize = 18.sp,
                fontWeight = FontWeight.ExtraBold,
                color = TrackingColors.Ink,
                textAlign = TextAlign.Center
            )
        }
        Text(
            text = detail,
            fontSize = 13.sp,
            color = TrackingColors.Muted,
            modifier = Modifier.padding(top = 2.dp)
        )
    }
}
