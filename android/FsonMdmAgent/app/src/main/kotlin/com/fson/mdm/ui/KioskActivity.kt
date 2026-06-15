package com.fson.mdm.ui

import android.os.Bundle
import android.view.KeyEvent
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import com.fson.mdm.R
import com.fson.mdm.core.Prefs
import com.fson.mdm.data.remote.dto.PolicyConfigDto
import com.fson.mdm.databinding.ActivityKioskBinding
import com.fson.mdm.device.KioskManager
import com.fson.mdm.device.PolicyEnforcer
import com.google.gson.Gson

/**
 * Full-screen kiosk shell. On entry it engages lock-task (single-app) mode and
 * presents only the policy's allowed apps as launch tiles. Back/recents are
 * suppressed; the device can only run whitelisted apps.
 */
class KioskActivity : AppCompatActivity() {

    private lateinit var binding: ActivityKioskBinding
    private lateinit var kiosk: KioskManager
    private lateinit var enforcer: PolicyEnforcer
    private lateinit var prefs: Prefs

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityKioskBinding.inflate(layoutInflater)
        setContentView(binding.root)

        kiosk = KioskManager(this)
        enforcer = PolicyEnforcer(this)
        prefs = Prefs.get(this)

        kiosk.startKiosk(this)
        renderAllowedApps()
    }

    override fun onResume() {
        super.onResume()
        // Re-assert lock-task in case the activity was relaunched.
        if (prefs.kioskActive) kiosk.startKiosk(this)
        renderAllowedApps()
    }

    private fun renderAllowedApps() {
        val container = binding.allowedAppsContainer
        container.removeAllViews()

        val config = readPolicyConfig()
        val apps = config?.allowedApps.orEmpty()
        val resolved = enforcer.resolveAllowedAppLabels(apps)

        if (resolved.isEmpty()) {
            val empty = TextView(this).apply {
                text = getString(R.string.kiosk_no_apps)
                setTextColor(ContextCompat.getColor(this@KioskActivity, R.color.fson_text_dim))
                textSize = 14f
            }
            container.addView(empty)
            return
        }

        for ((label, pkg) in resolved) {
            val btn = Button(this).apply {
                text = label
                setBackgroundColor(ContextCompat.getColor(this@KioskActivity, R.color.fson_copper))
                setTextColor(ContextCompat.getColor(this@KioskActivity, R.color.white))
                layoutParams = LinearLayout.LayoutParams(
                    LinearLayout.LayoutParams.MATCH_PARENT,
                    LinearLayout.LayoutParams.WRAP_CONTENT
                ).apply { topMargin = dp(10) }
                setOnClickListener {
                    enforcer.launchIntentFor(pkg)?.let { startActivity(it) }
                }
            }
            container.addView(btn)
        }
    }

    private fun readPolicyConfig(): PolicyConfigDto? {
        val json = prefs.lastPolicyJson ?: return null
        return runCatching { Gson().fromJson(json, PolicyConfigDto::class.java) }.getOrNull()
    }

    /** Block the back button while kiosked. */
    @Deprecated("Deprecated in Java")
    override fun onBackPressed() {
        if (prefs.kioskActive) return
        @Suppress("DEPRECATION")
        super.onBackPressed()
    }

    /** Swallow hardware keys that could escape the kiosk. */
    override fun onKeyDown(keyCode: Int, event: KeyEvent?): Boolean {
        if (prefs.kioskActive && (keyCode == KeyEvent.KEYCODE_HOME || keyCode == KeyEvent.KEYCODE_APP_SWITCH)) {
            return true
        }
        return super.onKeyDown(keyCode, event)
    }

    private fun dp(value: Int): Int = (value * resources.displayMetrics.density).toInt()
}
