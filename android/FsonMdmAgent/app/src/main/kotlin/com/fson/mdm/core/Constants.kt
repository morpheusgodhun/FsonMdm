package com.fson.mdm.core

/**
 * Central place for tunable constants and the policy/command contract strings
 * that must stay in lock-step with the backend.
 */
object Constants {

    // Heartbeat / policy polling cadence.
    const val HEARTBEAT_INTERVAL_SECONDS = 45L

    // WorkManager periodic minimum is 15 minutes; we use it as a safety-net
    // re-arm in case the foreground service is killed.
    const val WORKER_INTERVAL_MINUTES = 15L

    // Report installed-app inventory at most this often (ms) to avoid churn.
    const val APP_INVENTORY_MIN_INTERVAL_MS = 6L * 60 * 60 * 1000 // 6 hours

    // Notification channel + id for the foreground heartbeat service.
    const val HEARTBEAT_CHANNEL_ID = "fson_mdm_heartbeat"
    const val HEARTBEAT_NOTIFICATION_ID = 1001
    const val MESSAGE_CHANNEL_ID = "fson_mdm_messages"
    const val MESSAGE_NOTIFICATION_BASE_ID = 2000

    // Screen-capture foreground service channel + id.
    const val CAPTURE_CHANNEL_ID = "fson_mdm_capture"
    const val CAPTURE_NOTIFICATION_ID = 1002

    // WorkManager unique names.
    const val WORK_HEARTBEAT = "fson_mdm_heartbeat_work"
    const val WORK_POLICY = "fson_mdm_policy_work"

    // Command types — must match backend CommandService wire tokens.
    const val CMD_LOCK = "LOCK"
    const val CMD_MESSAGE = "MESSAGE"
    const val CMD_RESTART = "RESTART"
    const val CMD_INSTALL_APK = "INSTALL_APK"
    const val CMD_REQUEST_LOCATION = "REQUEST_LOCATION"
    const val CMD_SCREENSHOT = "SCREENSHOT"

    // Command ack statuses — must match backend (SENT|DONE).
    const val STATUS_SENT = "SENT"
    const val STATUS_DONE = "DONE"

    // System packages targeted by the policy engine.
    const val PKG_SETTINGS = "com.android.settings"
    const val PKG_PLAY_STORE = "com.android.vending"
}
