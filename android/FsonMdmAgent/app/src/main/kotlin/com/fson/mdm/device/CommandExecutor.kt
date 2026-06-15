package com.fson.mdm.device

import android.app.NotificationManager
import android.app.admin.DevicePolicyManager
import android.content.Context
import android.os.Build
import android.util.Log
import androidx.core.app.NotificationCompat
import com.fson.mdm.R
import com.fson.mdm.core.Constants
import com.fson.mdm.data.MdmRepository
import com.fson.mdm.data.MdmResult
import com.fson.mdm.service.ScreenCaptureService

/**
 * Executes one-time remote commands. Returns true when the action was carried
 * out so the caller can ACK the command as DONE. Network-bound commands
 * (location, APK install, screenshot) run on the calling coroutine.
 */
class CommandExecutor(context: Context) {

    private val appContext = context.applicationContext
    private val dpm = appContext.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
    private val admin = MdmDeviceAdminReceiver.componentName(appContext)
    private val repo = MdmRepository(appContext)

    private val isDeviceOwner: Boolean get() = dpm.isDeviceOwnerApp(appContext.packageName)
    private val isAdminActive: Boolean get() = dpm.isAdminActive(admin)

    suspend fun execute(type: String, payload: String?): Boolean = when (type.trim().uppercase()) {
        Constants.CMD_LOCK -> lock()
        Constants.CMD_MESSAGE -> message(payload)
        Constants.CMD_RESTART -> restart()
        Constants.CMD_INSTALL_APK -> installApk(payload)
        Constants.CMD_REQUEST_LOCATION -> reportLocation()
        Constants.CMD_SCREENSHOT -> screenshot()
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

    /** Downloads the managed APK identified by [payload] (app id) and installs it silently. */
    private suspend fun installApk(payload: String?): Boolean {
        val appId = payload?.trim()
        if (appId.isNullOrBlank()) {
            Log.w(TAG, "INSTALL_APK: appId payload eksik")
            return false
        }
        val file = repo.downloadApk(appId)
        if (file == null) {
            Log.w(TAG, "INSTALL_APK: APK indirilemedi")
            return false
        }
        return ApkInstaller(appContext).install(file)
    }

    /** Captures a single location fix and reports it immediately. */
    private suspend fun reportLocation(): Boolean {
        val location = LocationProvider(appContext).currentLocation()
        if (location == null) {
            Log.w(TAG, "REQUEST_LOCATION: konum alınamadı (izin/sağlayıcı yok)")
            return false
        }
        val acc = if (location.hasAccuracy()) location.accuracy.toDouble() else null
        return when (repo.reportLocation(location.latitude, location.longitude, acc)) {
            is MdmResult.Ok -> true
            is MdmResult.Err -> false
        }
    }

    /**
     * Triggers a remote screenshot if the operator has granted MediaProjection
     * consent (once per app run via MainActivity). The capture + upload happen
     * in [ScreenCaptureService].
     */
    private fun screenshot(): Boolean {
        if (!MediaProjectionHolder.hasConsent) {
            Log.w(TAG, "SCREENSHOT: ekran yakalama izni verilmemiş")
            return false
        }
        ScreenCaptureService.start(appContext)
        return true
    }

    companion object {
        private const val TAG = "CommandExecutor"
    }
}
