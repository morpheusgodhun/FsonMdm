package com.fson.mdm

import android.app.Application
import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build
import com.fson.mdm.core.Constants

/**
 * Application entry point. Creates notification channels up-front so the
 * foreground heartbeat service and command messages can post immediately.
 */
class FsonMdmApp : Application() {

    override fun onCreate() {
        super.onCreate()
        createChannels()
    }

    private fun createChannels() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val nm = getSystemService(NotificationManager::class.java)

        val heartbeat = NotificationChannel(
            Constants.HEARTBEAT_CHANNEL_ID,
            getString(R.string.heartbeat_channel_name),
            NotificationManager.IMPORTANCE_LOW
        ).apply { description = getString(R.string.heartbeat_channel_desc) }

        val messages = NotificationChannel(
            Constants.MESSAGE_CHANNEL_ID,
            getString(R.string.heartbeat_channel_name),
            NotificationManager.IMPORTANCE_HIGH
        )

        nm.createNotificationChannel(heartbeat)
        nm.createNotificationChannel(messages)
    }
}
