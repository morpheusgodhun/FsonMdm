package com.fson.mdm.device

import android.app.admin.DeviceAdminReceiver
import android.content.ComponentName
import android.content.Context
import android.content.Intent

/**
 * Device admin / device owner receiver. The device is assumed to already have
 * this component granted as Device Owner (enrollment is out of scope), so this
 * class mainly provides the [ComponentName] used by [PolicyEnforcer] and friends.
 */
class MdmDeviceAdminReceiver : DeviceAdminReceiver() {

    override fun onEnabled(context: Context, intent: Intent) {
        super.onEnabled(context, intent)
        // No-op: enrollment is handled outside this agent.
    }

    companion object {
        fun componentName(context: Context): ComponentName =
            ComponentName(context.applicationContext, MdmDeviceAdminReceiver::class.java)
    }
}
