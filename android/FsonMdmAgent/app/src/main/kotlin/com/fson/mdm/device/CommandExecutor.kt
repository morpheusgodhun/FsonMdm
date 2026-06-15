package com.fson.mdm.device

import android.app.NotificationManager
import android.app.admin.DevicePolicyManager
import android.content.Context
import android.os.Build
import android.util.Log
import androidx.core.app.NotificationCompat
import com.fson.mdm.R
import com.fson.mdm.core.Constants

/**
 * Executes one-time remote commands. Returns true when the action was carried
 * out so the caller can ACK the command as DONE.
 */
class CommandExecutor(context: Context) {

    private val appContext = context.applicationContext
    private val dpm = appContext.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
    private val admin = MdmDeviceAdminReceiver.componentName(appContext)

    private val isDeviceOwner: Boolean get() = dpm.isDeviceOwnerApp(appContext.packageName)
    private val isAdminActive: Boolean get() = dpm.isAdminActive(admin)

    fun execute(type: String, payload: String?): Boolean = when (type.trim().uppercase()) {
        Constants.CMD_LOCK -> lock()
        Constants.CMD_MESSAGE -> message(payload)
        Constants.CMD_RESTART -> restart()
        else -> {
            Log.w(TAG, "Bilinmeyen komut: $type")
            false
        }
    }

    /** Immediately locks the screen. Requires an active device admin. */
    private fun lock(): Boolean = try {
        if (isAdminActive || isDeviceOwner) {
            dpm.lockNow()
            true
        } else false
    } catch (e: Exception) {
        Log.w(TAG, "lock hata: ${e.message}"); false
    }

    /** Surfaces a high-priority notification carrying the admin message. */
    private fun message(payload: String?): Boolean = try {
        val text = payload?.takeIf { it.isNotBlank() } ?: "Yöneticiden mesaj"
        val nm = appContext.getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        val notification = NotificationCompat.Builder(appContext, Constants.MESSAGE_CHANNEL_ID)
            .setSmallIcon(R.mipmap.ic_launcher)
            .setContentTitle(appContext.getString(R.string.app_name))
            .setContentText(text)
            .setStyle(NotificationCompat.BigTextStyle().bigText(text))
            .setPriority(NotificationCompat.PRIORITY_HIGH)
            .setAutoCancel(true)
            .build()
        nm.notify(Constants.MESSAGE_NOTIFICATION_BASE_ID + (System.currentTimeMillis() % 1000).toInt(), notification)
        true
    } catch (e: Exception) {
        Log.w(TAG, "message hata: ${e.message}"); false
    }

    /** Reboots the device. Device Owner only; no-op otherwise. */
    private fun restart(): Boolean = try {
        if (isDeviceOwner && Build.VERSION.SDK_INT >= Build.VERSION_CODES.N) {
            dpm.reboot(admin)
            true
        } else false
    } catch (e: Exception) {
        Log.w(TAG, "restart hata: ${e.message}"); false
    }

    companion object {
        private const val TAG = "CommandExecutor"
    }
}
