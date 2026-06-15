package com.fson.mdm.device

import android.app.Activity
import android.app.ActivityManager
import android.app.admin.DevicePolicyManager
import android.content.ComponentName
import android.content.Context
import android.content.IntentFilter
import android.os.Build
import android.util.Log
import com.fson.mdm.ui.MainActivity

/**
 * Owns kiosk / lock-task lifecycle: making the agent the persistent HOME
 * activity and entering/exiting the Android lock-task (single-app) state.
 */
class KioskManager(context: Context) {

    private val appContext = context.applicationContext
    private val dpm = appContext.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
    private val admin = MdmDeviceAdminReceiver.componentName(appContext)

    private val isDeviceOwner: Boolean get() = dpm.isDeviceOwnerApp(appContext.packageName)

    /**
     * Registers this app as the default HOME so the user cannot leave to a
     * launcher while kiosked. Only the Device Owner may do this.
     */
    fun setAsHomeLauncher() = safe("setHome") {
        if (!isDeviceOwner) return@safe
        val filter = IntentFilter(android.content.Intent.ACTION_MAIN).apply {
            addCategory(android.content.Intent.CATEGORY_HOME)
            addCategory(android.content.Intent.CATEGORY_DEFAULT)
        }
        val activity = ComponentName(appContext, MainActivity::class.java)
        dpm.addPersistentPreferredActivity(admin, filter, activity)
    }

    fun clearHomeLauncher() = safe("clearHome") {
        if (!isDeviceOwner) return@safe
        dpm.clearPackagePersistentPreferredActivities(admin, appContext.packageName)
    }

    /** Starts lock-task for the given activity (call from the activity itself). */
    fun startKiosk(activity: Activity) = safe("startKiosk") {
        if (!isInLockTask(activity)) {
            activity.startLockTask()
        }
    }

    fun stopKiosk(activity: Activity) = safe("stopKiosk") {
        if (isInLockTask(activity)) {
            activity.stopLockTask()
        }
    }

    private fun isInLockTask(context: Context): Boolean {
        val am = context.getSystemService(Context.ACTIVITY_SERVICE) as ActivityManager
        return if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
            am.lockTaskModeState != ActivityManager.LOCK_TASK_MODE_NONE
        } else {
            @Suppress("DEPRECATION")
            am.isInLockTaskMode
        }
    }

    private inline fun safe(tag: String, block: () -> Unit) {
        try {
            block()
        } catch (e: Exception) {
            Log.w(TAG, "Kiosk hata ($tag): ${e.message}")
        }
    }

    companion object {
        private const val TAG = "KioskManager"
    }
}
