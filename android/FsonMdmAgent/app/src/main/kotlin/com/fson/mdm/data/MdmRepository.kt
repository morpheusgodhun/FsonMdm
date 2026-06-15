package com.fson.mdm.data

import android.content.Context
import android.os.Build
import android.provider.Settings
import com.fson.mdm.core.Prefs
import com.fson.mdm.data.remote.ApiClient
import com.fson.mdm.data.remote.dto.AckRequest
import com.fson.mdm.data.remote.dto.CommandDto
import com.fson.mdm.data.remote.dto.HeartbeatRequest
import com.fson.mdm.data.remote.dto.PolicyDto
import com.fson.mdm.data.remote.dto.RegisterRequest
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.net.Inet4Address
import java.net.NetworkInterface

/** Simple success/error result so callers can react without try/catch noise. */
sealed class MdmResult<out T> {
    data class Ok<T>(val value: T) : MdmResult<T>()
    data class Err(val message: String) : MdmResult<Nothing>()
}

/**
 * Single point of contact with the backend. Owns the register handshake,
 * heartbeat, policy fetch, and command poll/ack — all on the IO dispatcher.
 */
class MdmRepository(context: Context) {

    private val appContext = context.applicationContext
    private val prefs = Prefs.get(appContext)
    private val api get() = ApiClient.service(prefs)

    /**
     * Stable hardware-ish identifier. ANDROID_ID is per-app+device and survives
     * app updates, which is enough to key a managed device in this MVP.
     */
    @Suppress("HardwareIds")
    fun resolveDeviceId(): String {
        prefs.deviceId?.let { return it }
        val androidId = Settings.Secure.getString(
            appContext.contentResolver, Settings.Secure.ANDROID_ID
        )
        val id = if (androidId.isNullOrBlank())
            "FSON-${runCatching { Build.SERIAL }.getOrDefault("UNKNOWN")}"
        else "FSON-$androidId"
        prefs.deviceId = id
        return id
    }

    fun deviceModel(): String = "${Build.MANUFACTURER} ${Build.MODEL}".trim()

    /** Best-effort local IPv4 for display/inventory purposes. */
    fun localIpAddress(): String? = runCatching {
        NetworkInterface.getNetworkInterfaces().toList()
            .flatMap { it.inetAddresses.toList() }
            .firstOrNull { !it.isLoopbackAddress && it is Inet4Address }
            ?.hostAddress
    }.getOrNull()

    suspend fun register(): MdmResult<PolicyDto?> = withContext(Dispatchers.IO) {
        try {
            val body = RegisterRequest(
                deviceId = resolveDeviceId(),
                model = deviceModel(),
                ipAddress = localIpAddress()
            )
            val res = api.register(prefs.enrollmentKey, body)
            if (!res.isSuccessful) return@withContext MdmResult.Err(errorText(res.code(), res.errorBody()?.string()))

            val data = res.body() ?: return@withContext MdmResult.Err("Boş kayıt yanıtı.")
            prefs.token = data.token
            prefs.deviceId = data.deviceId
            prefs.serverDeviceGuid = data.id

            // Immediately pull policy so the device starts enforced.
            when (val p = fetchPolicy()) {
                is MdmResult.Ok -> MdmResult.Ok(p.value)
                is MdmResult.Err -> MdmResult.Ok(null) // registered, policy can retry later
            }
        } catch (e: Exception) {
            MdmResult.Err(e.localizedMessage ?: "Ağ hatası")
        }
    }

    suspend fun heartbeat(): MdmResult<Unit> = withContext(Dispatchers.IO) {
        if (!prefs.isRegistered) return@withContext MdmResult.Err("Kayıtlı değil")
        try {
            val res = api.heartbeat(HeartbeatRequest(localIpAddress()))
            if (res.isSuccessful) MdmResult.Ok(Unit)
            else MdmResult.Err(errorText(res.code(), res.errorBody()?.string()))
        } catch (e: Exception) {
            MdmResult.Err(e.localizedMessage ?: "Ağ hatası")
        }
    }

    suspend fun fetchPolicy(): MdmResult<PolicyDto> = withContext(Dispatchers.IO) {
        val deviceId = prefs.deviceId ?: return@withContext MdmResult.Err("Cihaz kimliği yok")
        try {
            val res = api.getPolicy(deviceId)
            if (!res.isSuccessful) return@withContext MdmResult.Err(errorText(res.code(), res.errorBody()?.string()))
            val data = res.body() ?: return@withContext MdmResult.Err("Boş politika yanıtı.")
            MdmResult.Ok(data)
        } catch (e: Exception) {
            MdmResult.Err(e.localizedMessage ?: "Ağ hatası")
        }
    }

    suspend fun pendingCommands(): MdmResult<List<CommandDto>> = withContext(Dispatchers.IO) {
        val deviceId = prefs.deviceId ?: return@withContext MdmResult.Err("Cihaz kimliği yok")
        try {
            val res = api.pendingCommands(deviceId)
            if (!res.isSuccessful) return@withContext MdmResult.Err(errorText(res.code(), res.errorBody()?.string()))
            MdmResult.Ok(res.body() ?: emptyList())
        } catch (e: Exception) {
            MdmResult.Err(e.localizedMessage ?: "Ağ hatası")
        }
    }

    suspend fun ack(commandId: String, status: String): MdmResult<Unit> = withContext(Dispatchers.IO) {
        try {
            val res = api.ackCommand(AckRequest(commandId, status))
            if (res.isSuccessful) MdmResult.Ok(Unit)
            else MdmResult.Err(errorText(res.code(), res.errorBody()?.string()))
        } catch (e: Exception) {
            MdmResult.Err(e.localizedMessage ?: "Ağ hatası")
        }
    }

    private fun errorText(code: Int, body: String?): String =
        if (body.isNullOrBlank()) "HTTP $code" else "HTTP $code: ${body.take(180)}"
}
