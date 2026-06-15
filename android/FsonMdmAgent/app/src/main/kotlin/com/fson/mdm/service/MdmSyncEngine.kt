package com.fson.mdm.service

import android.content.Context
import android.util.Log
import com.fson.mdm.core.Constants
import com.fson.mdm.core.Prefs
import com.fson.mdm.data.MdmRepository
import com.fson.mdm.data.MdmResult
import com.fson.mdm.device.CommandExecutor
import com.fson.mdm.device.PolicyEnforcer
import com.google.gson.Gson

/**
 * One full management cycle: heartbeat -> fetch & enforce policy -> drain
 * pending commands. Shared by the foreground service loop and the WorkManager
 * safety-net so the behaviour is identical regardless of trigger.
 */
class MdmSyncEngine(context: Context) {

    private val appContext = context.applicationContext
    private val prefs = Prefs.get(appContext)
    private val repo = MdmRepository(appContext)
    private val enforcer = PolicyEnforcer(appContext)
    private val executor = CommandExecutor(appContext)
    private val gson = Gson()

    /** Runs a complete cycle. Safe to call repeatedly; tolerates partial failures. */
    suspend fun runCycle() {
        if (!prefs.isRegistered) {
            Log.d(TAG, "Kayıtlı değil — döngü atlandı.")
            return
        }
        sendHeartbeat()
        enforcePolicy()
        drainCommands()
    }

    private suspend fun sendHeartbeat() {
        when (val r = repo.heartbeat()) {
            is MdmResult.Ok -> Log.d(TAG, "Heartbeat OK")
            is MdmResult.Err -> Log.w(TAG, "Heartbeat hata: ${r.message}")
        }
    }

    /** Pulls the active policy and applies it. Falls back to the cached policy. */
    suspend fun enforcePolicy() {
        when (val r = repo.fetchPolicy()) {
            is MdmResult.Ok -> {
                val policy = r.value
                prefs.lastPolicyJson = gson.toJson(policy.config)
                prefs.lastPolicyVersion = policy.version
                enforcer.apply(policy.config)
                prefs.kioskActive = policy.config.kioskMode
                Log.d(TAG, "Politika uygulandı v${policy.version}")
            }
            is MdmResult.Err -> {
                Log.w(TAG, "Politika alınamadı: ${r.message} — önbellek kullanılıyor")
                applyCachedPolicy()
            }
        }
    }

    private fun applyCachedPolicy() {
        val json = prefs.lastPolicyJson ?: return
        runCatching {
            val config = gson.fromJson(json, com.fson.mdm.data.remote.dto.PolicyConfigDto::class.java)
            enforcer.apply(config)
        }.onFailure { Log.w(TAG, "Önbellek politikası uygulanamadı: ${it.message}") }
    }

    private suspend fun drainCommands() {
        val pending = when (val r = repo.pendingCommands()) {
            is MdmResult.Ok -> r.value
            is MdmResult.Err -> {
                Log.w(TAG, "Komut çekme hata: ${r.message}")
                return
            }
        }
        for (cmd in pending) {
            // Mark as received first so the backend stops re-serving it.
            repo.ack(cmd.id, Constants.STATUS_SENT)
            val ok = executor.execute(cmd.type, cmd.payload)
            if (ok) {
                repo.ack(cmd.id, Constants.STATUS_DONE)
                Log.d(TAG, "Komut ${cmd.type} tamamlandı")
            } else {
                Log.w(TAG, "Komut ${cmd.type} çalıştırılamadı")
            }
        }
    }

    companion object {
        private const val TAG = "MdmSyncEngine"
    }
}
