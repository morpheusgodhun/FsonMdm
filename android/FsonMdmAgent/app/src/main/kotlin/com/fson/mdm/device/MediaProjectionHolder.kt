package com.fson.mdm.device

import android.content.Intent

/**
 * Holds the one-time MediaProjection consent result granted by the operator in
 * MainActivity. A projection token cannot be persisted across process death, so
 * remote screenshots require the operator to (re)grant consent once per app run.
 */
object MediaProjectionHolder {

    @Volatile var resultCode: Int = 0
        private set

    @Volatile var resultData: Intent? = null
        private set

    val hasConsent: Boolean get() = resultData != null

    fun set(resultCode: Int, data: Intent) {
        this.resultCode = resultCode
        this.resultData = data
    }

    fun clear() {
        resultCode = 0
        resultData = null
    }
}
