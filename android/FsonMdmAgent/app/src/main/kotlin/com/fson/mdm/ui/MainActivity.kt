package com.fson.mdm.ui

import android.Manifest
import android.content.Intent
import android.os.Build
import android.os.Bundle
import android.view.Gravity
import android.widget.Button
import android.widget.LinearLayout
import android.widget.TextView
import android.widget.Toast
import androidx.activity.result.contract.ActivityResultContracts
import androidx.appcompat.app.AppCompatActivity
import androidx.core.content.ContextCompat
import androidx.lifecycle.lifecycleScope
import com.fson.mdm.R
import com.fson.mdm.core.Prefs
import com.fson.mdm.data.MdmRepository
import com.fson.mdm.data.MdmResult
import com.fson.mdm.databinding.ActivityMainBinding
import com.fson.mdm.device.KioskManager
import com.fson.mdm.device.PolicyEnforcer
import com.fson.mdm.permission.PermissionManager
import com.fson.mdm.service.HeartbeatService
import com.fson.mdm.service.MdmSyncEngine
import com.fson.mdm.service.PolicyWorker
import kotlinx.coroutines.launch

/**
 * Agent control surface: shows Device Owner status, lets the operator set the
 * server address + enrollment key and register, exposes the required permission
 * grant flow, and provides manual policy refresh / kiosk toggle for testing.
 */
class MainActivity : AppCompatActivity() {

    private lateinit var binding: ActivityMainBinding
    private lateinit var prefs: Prefs
    private lateinit var repo: MdmRepository
    private lateinit var permissions: PermissionManager
    private lateinit var enforcer: PolicyEnforcer
    private lateinit var kiosk: KioskManager

    private val notificationPermissionLauncher =
        registerForActivityResult(ActivityResultContracts.RequestPermission()) {
            renderPermissions()
        }

    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        binding = ActivityMainBinding.inflate(layoutInflater)
        setContentView(binding.root)

        prefs = Prefs.get(this)
        repo = MdmRepository(this)
        permissions = PermissionManager(this)
        enforcer = PolicyEnforcer(this)
        kiosk = KioskManager(this)

        binding.editBaseUrl.setText(prefs.baseUrl)
        binding.editEnrollmentKey.setText(prefs.enrollmentKey)

        binding.btnRegister.setOnClickListener { onRegisterClicked() }
        binding.btnRefreshPolicy.setOnClickListener { onRefreshPolicyClicked() }
        binding.btnKiosk.setOnClickListener { onKioskToggleClicked() }

        // If already registered, make sure management is running.
        if (prefs.isRegistered) {
            HeartbeatService.start(this)
            PolicyWorker.schedule(this)
        }
    }

    override fun onResume() {
        super.onResume()
        renderDeviceOwner()
        renderRegisterState()
        renderPermissions()
        renderPolicyState()
        renderKioskButton()
    }

    // ---- Register ----

    private fun onRegisterClicked() {
        persistConnectionFields()
        binding.txtRegisterState.text = getString(R.string.msg_registering)
        binding.btnRegister.isEnabled = false

        lifecycleScope.launch {
            val result = repo.register()
            binding.btnRegister.isEnabled = true
            when (result) {
                is MdmResult.Ok -> {
                    HeartbeatService.start(this@MainActivity)
                    PolicyWorker.schedule(this@MainActivity)
                    toast(getString(R.string.msg_register_ok))
                    val applied = result.value
                    if (applied != null) {
                        enforcer.apply(applied.config)
                        prefs.kioskActive = applied.config.kioskMode
                        toast(getString(R.string.msg_policy_applied, applied.version))
                    }
                    renderRegisterState(); renderPolicyState(); renderKioskButton()
                }
                is MdmResult.Err -> {
                    toast(getString(R.string.msg_register_fail, result.message))
                    renderRegisterState()
                }
            }
        }
    }

    private fun persistConnectionFields() {
        val url = binding.editBaseUrl.text?.toString().orEmpty().ifBlank { prefs.baseUrl }
        val key = binding.editEnrollmentKey.text?.toString().orEmpty().ifBlank { prefs.enrollmentKey }
        prefs.baseUrl = url
        prefs.enrollmentKey = key
        com.fson.mdm.data.remote.ApiClient.invalidate()
    }

    // ---- Policy ----

    private fun onRefreshPolicyClicked() {
        if (!prefs.isRegistered) {
            toast(getString(R.string.msg_need_register)); return
        }
        binding.btnRefreshPolicy.isEnabled = false
        lifecycleScope.launch {
            // Run a full cycle so commands are also drained on demand.
            runCatching { MdmSyncEngine(this@MainActivity).runCycle() }
            binding.btnRefreshPolicy.isEnabled = true
            renderPolicyState(); renderKioskButton()
            val v = prefs.lastPolicyVersion
            if (v > 0) toast(getString(R.string.msg_policy_applied, v))
        }
    }

    // ---- Kiosk ----

    private fun onKioskToggleClicked() {
        if (prefs.kioskActive) {
            kiosk.stopKiosk(this)
            kiosk.clearHomeLauncher()
            prefs.kioskActive = false
            toast(getString(R.string.msg_kiosk_off))
        } else {
            kiosk.setAsHomeLauncher()
            prefs.kioskActive = true
            startActivity(Intent(this, KioskActivity::class.java))
            toast(getString(R.string.msg_kiosk_on))
        }
        renderKioskButton()
    }

    // ---- Rendering ----

    private fun renderDeviceOwner() {
        binding.txtDeviceOwner.text = getString(
            if (enforcer.isDeviceOwner) R.string.state_device_owner_yes
            else R.string.state_device_owner_no
        )
    }

    private fun renderRegisterState() {
        val registered = prefs.isRegistered
        binding.txtRegisterState.text = getString(
            if (registered) R.string.state_registered else R.string.state_not_registered
        )
        binding.txtRegisterState.setTextColor(
            ContextCompat.getColor(this, if (registered) R.color.fson_green else R.color.fson_red)
        )
    }

    private fun renderPolicyState() {
        val v = prefs.lastPolicyVersion
        binding.txtPolicyState.text = if (v > 0) "Politika sürümü: v$v" else "Politika: -"
    }

    private fun renderKioskButton() {
        binding.btnKiosk.text = getString(
            if (prefs.kioskActive) R.string.btn_stop_kiosk else R.string.btn_start_kiosk
        )
    }

    private fun renderPermissions() {
        val container = binding.permissionContainer
        container.removeAllViews()
        addPermissionRow(container, getString(R.string.perm_notifications), PermissionManager.PermissionType.NOTIFICATIONS)
        addPermissionRow(container, getString(R.string.perm_battery), PermissionManager.PermissionType.BATTERY)
        addPermissionRow(container, getString(R.string.perm_usage), PermissionManager.PermissionType.USAGE_ACCESS)
        addPermissionRow(container, getString(R.string.perm_overlay), PermissionManager.PermissionType.OVERLAY)
    }

    private fun addPermissionRow(
        container: LinearLayout,
        label: String,
        type: PermissionManager.PermissionType
    ) {
        val granted = permissions.isGranted(type)

        val row = LinearLayout(this).apply {
            orientation = LinearLayout.HORIZONTAL
            gravity = Gravity.CENTER_VERTICAL
            setPadding(0, dp(8), 0, dp(8))
        }

        val name = TextView(this).apply {
            text = label
            setTextColor(ContextCompat.getColor(this@MainActivity, R.color.fson_text))
            textSize = 14f
            layoutParams = LinearLayout.LayoutParams(0, LinearLayout.LayoutParams.WRAP_CONTENT, 1f)
        }

        val status = TextView(this).apply {
            text = getString(if (granted) R.string.perm_granted else R.string.perm_missing)
            setTextColor(
                ContextCompat.getColor(
                    this@MainActivity,
                    if (granted) R.color.fson_green else R.color.fson_red
                )
            )
            textSize = 13f
            setPadding(dp(8), 0, dp(8), 0)
        }

        row.addView(name)
        row.addView(status)

        if (!granted) {
            val grant = Button(this).apply {
                text = getString(R.string.perm_grant)
                textSize = 12f
                setOnClickListener { requestPermission(type) }
            }
            row.addView(grant)
        }

        container.addView(row)
    }

    private fun requestPermission(type: PermissionManager.PermissionType) {
        if (type == PermissionManager.PermissionType.NOTIFICATIONS) {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.TIRAMISU) {
                notificationPermissionLauncher.launch(Manifest.permission.POST_NOTIFICATIONS)
            }
            return
        }
        permissions.settingsIntentFor(type)?.let { intent ->
            runCatching { startActivity(intent) }
                .onFailure { toast("Ayar ekranı açılamadı: ${it.message}") }
        }
    }

    private fun dp(value: Int): Int = (value * resources.displayMetrics.density).toInt()

    private fun toast(message: String) =
        Toast.makeText(this, message, Toast.LENGTH_SHORT).show()
}
