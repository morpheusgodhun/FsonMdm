package com.fson.mdm.device

import android.Manifest
import android.content.Context
import android.content.pm.PackageManager
import android.location.Location
import android.location.LocationManager
import android.os.Build
import android.os.Looper
import android.util.Log
import androidx.core.content.ContextCompat
import kotlinx.coroutines.suspendCancellableCoroutine
import kotlinx.coroutines.withTimeoutOrNull
import kotlin.coroutines.resume

/**
 * Obtains a single location fix using the framework [LocationManager] only — no
 * Google Play Services dependency, so it works on AOSP builds and the emulator
 * (set a location via the emulator's extended controls).
 */
class LocationProvider(context: Context) {

    private val appContext = context.applicationContext
    private val lm = appContext.getSystemService(Context.LOCATION_SERVICE) as LocationManager

    fun hasPermission(): Boolean =
        ContextCompat.checkSelfPermission(appContext, Manifest.permission.ACCESS_FINE_LOCATION) ==
            PackageManager.PERMISSION_GRANTED ||
        ContextCompat.checkSelfPermission(appContext, Manifest.permission.ACCESS_COARSE_LOCATION) ==
            PackageManager.PERMISSION_GRANTED

    /**
     * Returns the freshest available fix, requesting a live update if no recent
     * cached one exists. Null when permission is missing or no provider responds.
     */
    suspend fun currentLocation(timeoutMs: Long = 12_000): Location? {
        if (!hasPermission()) {
            Log.w(TAG, "Konum izni yok")
            return null
        }

        val cached = lastKnown()
        // A cached fix under 2 minutes old is good enough; skip the live wait.
        if (cached != null && System.currentTimeMillis() - cached.time < 120_000) return cached

        val live = withTimeoutOrNull(timeoutMs) { requestSingle() }
        return live ?: cached
    }

    private fun lastKnown(): Location? {
        val providers = runCatching { lm.getProviders(true) }.getOrDefault(emptyList())
        var best: Location? = null
        for (p in providers) {
            val loc = try {
                @Suppress("MissingPermission") lm.getLastKnownLocation(p)
            } catch (e: SecurityException) { null }
            if (loc != null && (best == null || loc.time > best!!.time)) best = loc
        }
        return best
    }

    @Suppress("MissingPermission")
    private suspend fun requestSingle(): Location? = suspendCancellableCoroutine { cont ->
        val provider = when {
            lm.isProviderEnabled(LocationManager.GPS_PROVIDER) -> LocationManager.GPS_PROVIDER
            lm.isProviderEnabled(LocationManager.NETWORK_PROVIDER) -> LocationManager.NETWORK_PROVIDER
            else -> {
                if (cont.isActive) cont.resume(null)
                return@suspendCancellableCoroutine
            }
        }

        try {
            val listener = object : android.location.LocationListener {
                override fun onLocationChanged(location: Location) {
                    runCatching { lm.removeUpdates(this) }
                    if (cont.isActive) cont.resume(location)
                }
                override fun onProviderDisabled(provider: String) {}
                override fun onProviderEnabled(provider: String) {}
                @Deprecated("Deprecated in Java")
                override fun onStatusChanged(provider: String?, status: Int, extras: android.os.Bundle?) {}
            }

            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.R) {
                lm.requestLocationUpdates(provider, 0L, 0f, listener, Looper.getMainLooper())
            } else {
                lm.requestLocationUpdates(provider, 0L, 0f, listener, Looper.getMainLooper())
            }

            cont.invokeOnCancellation { runCatching { lm.removeUpdates(listener) } }
        } catch (e: Exception) {
            Log.w(TAG, "Konum güncellemesi hatası: ${e.message}")
            if (cont.isActive) cont.resume(null)
        }
    }

    companion object {
        private const val TAG = "LocationProvider"
    }
}
