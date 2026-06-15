package com.fson.mdm.permission

import android.Manifest
import android.app.AppOpsManager
import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import android.net.Uri
import android.os.Build
import android.os.PowerManager
import android.provider.Settings

/**
 * Centralises the runtime/special permissions the agent needs and provides the
 * exact system-settings intent to grant each one. The UI renders a row per
 * [PermissionType] and routes the user to the relevant screen.
 */
class PermissionManager(context: Context) {

    private val appContext = context.applicationContext

    enum class PermissionType { NOTIFICATIONS, BATTERY, USAGE_ACCESS, OVERLAY }

    fun isGranted(type: PermissionType): Boolean = when (type) {
        PermissionType.NOTIFICATIONS -> notificationsGranted()
        PermissionType.BATTERY -> batteryIgnored()
        PermissionType.USAGE_ACCESS -> usageAccessGranted()
        PermissionType.OVERLAY -> overlayGranted()
    }

    private fun notificationsGranted(): Boolean {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.TIRAMISU) return true
        return appContext.checkSelfPermission(Manifest.permission.POST_NOTIFICATIONS) ==
            PackageManager.PERMISSION_GRANTED
    }

    private fun batteryIgnored(): Boolean {
        val pm = appContext.getSystemService(Context.POWER_SERVICE) as PowerManager
        return pm.isIgnoringBatteryOptimizations(appContext.packageName)
    }

    private fun usageAccessGranted(): Boolean {
        val appOps = appContext.getSystemService(Context.APP_OPS_SERVICE) as AppOpsManager
        val mode = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            appOps.unsafeCheckOpNoThrow(
                AppOpsManager.OPSTR_GET_USAGE_STATS,
                android.os.Process.myUid(),
                appContext.packageName
            )
        } else {
            @Suppress("DEPRECATION")
            appOps.checkOpNoThrow(
                AppOpsManager.OPSTR_GET_USAGE_STATS,
                android.os.Process.myUid(),
                appContext.packageName
            )
        }
        return mode == AppOpsManager.MODE_ALLOWED
    }

    private fun overlayGranted(): Boolean = Settings.canDrawOverlays(appContext)

    /**
     * Returns the intent that takes the user to the correct grant screen, or
     * null for NOTIFICATIONS which is handled via the runtime request API.
     */
    fun settingsIntentFor(type: PermissionType): Intent? = when (type) {
        PermissionType.NOTIFICATIONS -> null
        PermissionType.BATTERY -> Intent(
            Settings.ACTION_REQUEST_IGNORE_BATTERY_OPTIMIZATIONS,
            Uri.parse("package:${appContext.packageName}")
        )
        PermissionType.USAGE_ACCESS -> Intent(Settings.ACTION_USAGE_ACCESS_SETTINGS)
        PermissionType.OVERLAY -> Intent(
            Settings.ACTION_MANAGE_OVERLAY_PERMISSION,
            Uri.parse("package:${appContext.packageName}")
        )
    }
}
