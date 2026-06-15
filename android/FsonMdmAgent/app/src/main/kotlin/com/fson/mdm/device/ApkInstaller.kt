package com.fson.mdm.device

import android.app.PendingIntent
import android.content.Context
import android.content.Intent
import android.content.pm.PackageInstaller
import android.os.Build
import android.util.Log
import java.io.File

/**
 * Installs an APK from a local file using [PackageInstaller]. When the agent is
 * the Device Owner the install is silent (no user prompt). The session write +
 * commit is treated as success; the OS reports the final result asynchronously
 * to the supplied IntentSender (logged for diagnostics).
 */
class ApkInstaller(context: Context) {

    private val appContext = context.applicationContext

    fun install(apk: File): Boolean {
        if (!apk.exists() || apk.length() == 0L) {
            Log.w(TAG, "APK dosyası yok/boş: ${apk.absolutePath}")
            return false
        }

        val installer = appContext.packageManager.packageInstaller
        var sessionId = -1
        return try {
            val params = PackageInstaller.SessionParams(
                PackageInstaller.SessionParams.MODE_FULL_INSTALL
            )
            sessionId = installer.createSession(params)
            installer.openSession(sessionId).use { session ->
                apk.inputStream().use { input ->
                    session.openWrite("fson_apk", 0, apk.length()).use { output ->
                        input.copyTo(output)
                        session.fsync(output)
                    }
                }

                val intent = Intent(ACTION_INSTALL_RESULT).setPackage(appContext.packageName)
                val flags = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.S)
                    PendingIntent.FLAG_MUTABLE else 0
                val pi = PendingIntent.getBroadcast(appContext, sessionId, intent, flags)
                session.commit(pi.intentSender)
            }
            Log.d(TAG, "APK kurulum oturumu commit edildi (session=$sessionId)")
            true
        } catch (e: Exception) {
            Log.w(TAG, "APK kurulum hatası: ${e.message}")
            if (sessionId >= 0) runCatching { installer.abandonSession(sessionId) }
            false
        }
    }

    companion object {
        private const val TAG = "ApkInstaller"
        const val ACTION_INSTALL_RESULT = "com.fson.mdm.INSTALL_RESULT"
    }
}
