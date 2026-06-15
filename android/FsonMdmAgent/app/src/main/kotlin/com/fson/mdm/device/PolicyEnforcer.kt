package com.fson.mdm.device

import android.app.admin.DevicePolicyManager
import android.content.Context
import android.content.pm.PackageManager
import android.os.Build
import android.util.Log
import com.fson.mdm.core.Constants
import com.fson.mdm.data.remote.dto.PolicyConfigDto

/**
 * Translates a [PolicyConfigDto] into concrete DevicePolicyManager state.
 * All calls are guarded so the agent degrades gracefully when it is not the
 * Device Owner (e.g. during development on an unmanaged device).
 */
class PolicyEnforcer(context: Context) {

    private val appContext = context.applicationContext
    private val dpm = appContext.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
    private val admin = MdmDeviceAdminReceiver.componentName(appContext)

    val isDeviceOwner: Boolean get() = dpm.isDeviceOwnerApp(appContext.packageName)
    val isAdminActive: Boolean get() = dpm.isAdminActive(admin)

    /**
     * Applies the full policy. Order matters: lock-task allow-list is set before
     * kiosk is engaged so the home/allowed apps are already whitelisted.
     */
    fun apply(config: PolicyConfigDto) {
        if (!isDeviceOwner) {
            Log.w(TAG, "Device Owner değil — politika kısmen uygulanacak.")
        }
        applyCamera(config.blockCamera)
        applyAppHidden(Constants.PKG_PLAY_STORE, config.blockPlayStore)
        applyAppHidden(Constants.PKG_SETTINGS, config.blockSettings)
        applyLockTaskPackages(config.allowedApps)
    }

    private fun applyCamera(block: Boolean) = safe("camera") {
        if (isAdminActive) dpm.setCameraDisabled(admin, block)
    }

    private fun applyAppHidden(pkg: String, hidden: Boolean) = safe("hide:$pkg") {
        if (!isDeviceOwner) return@safe
        // Only act if the package actually exists on the device.
        val exists = runCatching {
            appContext.packageManager.getApplicationInfo(pkg, 0); true
        }.getOrDefault(false)
        if (exists) dpm.setApplicationHidden(admin, pkg, hidden)
    }

    /**
     * Builds the lock-task allow-list: the agent itself plus every allowed app.
     * This is what makes startLockTask() succeed for kiosk mode.
     */
    fun applyLockTaskPackages(allowedApps: List<String>) = safe("lockTaskPackages") {
        if (!isDeviceOwner) return@safe
        val packages = buildSet {
            add(appContext.packageName)
            addAll(allowedApps)
        }.toTypedArray()
        dpm.setLockTaskPackages(admin, packages)

        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
            dpm.setLockTaskFeatures(
                admin,
                DevicePolicyManager.LOCK_TASK_FEATURE_HOME or
                    DevicePolicyManager.LOCK_TASK_FEATURE_GLOBAL_ACTIONS or
                    DevicePolicyManager.LOCK_TASK_FEATURE_KEYGUARD
            )
        }
    }

    /** Resolves launchable allowed apps to (label, package) pairs for the kiosk UI. */
    fun resolveAllowedAppLabels(allowedApps: List<String>): List<Pair<String, String>> {
        val pm = appContext.packageManager
        return allowedApps.mapNotNull { pkg ->
            runCatching {
                val info = pm.getApplicationInfo(pkg, 0)
                pm.getApplicationLabel(info).toString() to pkg
            }.getOrNull()
        }
    }

    fun launchIntentFor(pkg: String) =
        appContext.packageManager.getLaunchIntentForPackage(pkg)

    private inline fun safe(tag: String, block: () -> Unit) {
        try {
            block()
        } catch (e: SecurityException) {
            Log.w(TAG, "Yetki yok ($tag): ${e.message}")
        } catch (e: Exception) {
            Log.w(TAG, "Hata ($tag): ${e.message}")
        }
    }

    companion object {
        private const val TAG = "PolicyEnforcer"
    }
}
