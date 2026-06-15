package com.fson.mdm.core

import android.content.Context
import androidx.core.content.edit
import com.fson.mdm.BuildConfig

/**
 * Thin SharedPreferences wrapper holding agent state:
 * server address, enrollment key, the device JWT, and the resolved hardware
 * device id. Persists the last applied policy so enforcement survives restarts.
 */
class Prefs private constructor(context: Context) {

    private val sp = context.applicationContext
        .getSharedPreferences("fson_mdm_prefs", Context.MODE_PRIVATE)

    var baseUrl: String
        get() = sp.getString(KEY_BASE_URL, BuildConfig.DEFAULT_BASE_URL) ?: BuildConfig.DEFAULT_BASE_URL
        set(value) = sp.edit { putString(KEY_BASE_URL, normalizeUrl(value)) }

    var enrollmentKey: String
        get() = sp.getString(KEY_ENROLLMENT, BuildConfig.DEFAULT_ENROLLMENT_KEY) ?: BuildConfig.DEFAULT_ENROLLMENT_KEY
        set(value) = sp.edit { putString(KEY_ENROLLMENT, value.trim()) }

    /** Device-scoped JWT returned by /api/device/register. */
    var token: String?
        get() = sp.getString(KEY_TOKEN, null)
        set(value) = sp.edit { putString(KEY_TOKEN, value) }

    /** Hardware device identifier used in policy/command paths. */
    var deviceId: String?
        get() = sp.getString(KEY_DEVICE_ID, null)
        set(value) = sp.edit { putString(KEY_DEVICE_ID, value) }

    /** Server GUID for this device (informational). */
    var serverDeviceGuid: String?
        get() = sp.getString(KEY_DEVICE_GUID, null)
        set(value) = sp.edit { putString(KEY_DEVICE_GUID, value) }

    /** Last applied policy as raw JSON, so enforcement can be re-applied offline. */
    var lastPolicyJson: String?
        get() = sp.getString(KEY_POLICY_JSON, null)
        set(value) = sp.edit { putString(KEY_POLICY_JSON, value) }

    var lastPolicyVersion: Int
        get() = sp.getInt(KEY_POLICY_VERSION, 0)
        set(value) = sp.edit { putInt(KEY_POLICY_VERSION, value) }

    var kioskActive: Boolean
        get() = sp.getBoolean(KEY_KIOSK, false)
        set(value) = sp.edit { putBoolean(KEY_KIOSK, value) }

    /** Epoch millis of the last installed-app inventory report (throttling). */
    var lastAppsReportAt: Long
        get() = sp.getLong(KEY_APPS_REPORTED_AT, 0L)
        set(value) = sp.edit { putLong(KEY_APPS_REPORTED_AT, value) }

    val isRegistered: Boolean get() = !token.isNullOrBlank() && !deviceId.isNullOrBlank()

    fun clearRegistration() = sp.edit {
        remove(KEY_TOKEN); remove(KEY_DEVICE_GUID)
    }

    companion object {
        private const val KEY_BASE_URL = "base_url"
        private const val KEY_ENROLLMENT = "enrollment_key"
        private const val KEY_TOKEN = "token"
        private const val KEY_DEVICE_ID = "device_id"
        private const val KEY_DEVICE_GUID = "device_guid"
        private const val KEY_POLICY_JSON = "policy_json"
        private const val KEY_POLICY_VERSION = "policy_version"
        private const val KEY_KIOSK = "kiosk_active"
        private const val KEY_APPS_REPORTED_AT = "apps_reported_at"

        @Volatile private var instance: Prefs? = null

        fun get(context: Context): Prefs =
            instance ?: synchronized(this) {
                instance ?: Prefs(context).also { instance = it }
            }

        /** Ensures a single trailing slash so Retrofit base URL is valid. */
        fun normalizeUrl(url: String): String {
            val t = url.trim()
            return if (t.endsWith("/")) t else "$t/"
        }
    }
}
