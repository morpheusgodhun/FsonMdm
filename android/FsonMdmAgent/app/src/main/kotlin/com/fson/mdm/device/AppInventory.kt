package com.fson.mdm.device

import android.content.Context
import android.content.Intent
import android.content.pm.PackageManager
import com.fson.mdm.data.remote.dto.DeviceAppItem

/**
 * Enumerates installed applications so the dashboard can offer them as kiosk
 * whitelist candidates. Launchable apps (those with a launcher entry) are
 * flagged; the rest are still reported for completeness.
 */
class AppInventory(context: Context) {

    private val pm = context.applicationContext.packageManager

    fun collect(): List<DeviceAppItem> {
        val launchable = launchablePackages()
        val packages = pm.getInstalledPackages(0)

        return packages.mapNotNull { pkg ->
            val appInfo = pkg.applicationInfo ?: return@mapNotNull null
            val pkgName = pkg.packageName ?: return@mapNotNull null
            val label = runCatching { pm.getApplicationLabel(appInfo).toString() }
                .getOrDefault(pkgName)
            DeviceAppItem(
                packageName = pkgName,
                appLabel = label,
                isLaunchable = launchable.contains(pkgName)
            )
        }.distinctBy { it.packageName }
            .sortedBy { it.appLabel.lowercase() }
    }

    private fun launchablePackages(): Set<String> {
        val intent = Intent(Intent.ACTION_MAIN).addCategory(Intent.CATEGORY_LAUNCHER)
        val resolved = pm.queryIntentActivities(intent, 0)
        return resolved.mapNotNull { it.activityInfo?.packageName }.toHashSet()
    }
}
