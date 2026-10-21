package com.saborator.tracking

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.activity.enableEdgeToEdge
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.saborator.tracking.ui.theme.TrackingTheme
import com.saborator.tracking.ui.tracking.OrderTrackingScreen

class MainActivity : ComponentActivity() {

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        enableEdgeToEdge()
        setContent {
            TrackingTheme {
                val navController = rememberNavController()
                NavHost(navController = navController, startDestination = ROUTE_TRACKING) {
                    composable(ROUTE_TRACKING) { OrderTrackingScreen() }
                }
            }
        }
    }

    private companion object {
        const val ROUTE_TRACKING = "tracking"
    }
}
