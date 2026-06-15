package com.fson.mdm.service

import android.content.BroadcastReceiver
import android.content.Context
import android.content.Intent
import com.fson.mdm.core.Prefs

/**
 * Re-arms the agent after a reboot so management resumes automatically without
 * the user reopening the app.
 */
class BootReceiver : BroadcastReceiver() {

    override fun onReceive(context: Context, intent: Intent) {
        val action = intent.action ?: return
        if (action != Intent.ACTION_BOOT_COMPLETED &&
            action != Intent.ACTION_LOCKED_BOOT_COMPLETED
        ) return

        if (!Prefs.get(context).isRegistered) return

        HeartbeatService.start(context)
        PolicyWorker.schedule(context)
    }
}
