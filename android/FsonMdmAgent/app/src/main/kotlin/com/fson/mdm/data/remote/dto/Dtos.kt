package com.fson.mdm.data.remote.dto

import com.google.gson.annotations.SerializedName

// ---- Device register ----

/** POST /api/device/register body. Tenant resolved via X-Enrollment-Token header. */
data class RegisterRequest(
    @SerializedName("deviceId") val deviceId: String,
    @SerializedName("model") val model: String,
    @SerializedName("ipAddress") val ipAddress: String?
)

data class RegisterResponse(
    @SerializedName("id") val id: String,
    @SerializedName("deviceId") val deviceId: String,
    @SerializedName("token") val token: String,
    @SerializedName("expiresAt") val expiresAt: String
)

// ---- Heartbeat ----

/** POST /api/device/heartbeat body. */
data class HeartbeatRequest(
    @SerializedName("ipAddress") val ipAddress: String?
)

// ---- Policy ----

/**
 * Mirrors the backend PolicyConfig contract one-to-one. These flags drive the
 * on-device PolicyEnforcer.
 */
data class PolicyConfigDto(
    @SerializedName("kioskMode") val kioskMode: Boolean = false,
    @SerializedName("blockCamera") val blockCamera: Boolean = false,
    @SerializedName("blockSettings") val blockSettings: Boolean = false,
    @SerializedName("blockPlayStore") val blockPlayStore: Boolean = false,
    @SerializedName("allowedApps") val allowedApps: List<String> = emptyList()
)

/** GET /api/policy/{deviceId} response. */
data class PolicyDto(
    @SerializedName("id") val id: String,
    @SerializedName("name") val name: String,
    @SerializedName("version") val version: Int,
    @SerializedName("config") val config: PolicyConfigDto,
    @SerializedName("updatedAt") val updatedAt: String
)

// ---- Commands ----

/** GET /api/command/pending/{deviceId} item. */
data class CommandDto(
    @SerializedName("id") val id: String,
    @SerializedName("type") val type: String,
    @SerializedName("payload") val payload: String?,
    @SerializedName("status") val status: String,
    @SerializedName("createdAt") val createdAt: String
)

/** POST /api/command/ack body. */
data class AckRequest(
    @SerializedName("commandId") val commandId: String,
    @SerializedName("status") val status: String
)

// ---- Location tracking ----

/** POST /api/device/location body. */
data class LocationReportRequest(
    @SerializedName("latitude") val latitude: Double,
    @SerializedName("longitude") val longitude: Double,
    @SerializedName("accuracy") val accuracy: Double?
)

// ---- Installed-app inventory ----

data class DeviceAppItem(
    @SerializedName("packageName") val packageName: String,
    @SerializedName("appLabel") val appLabel: String,
    @SerializedName("isLaunchable") val isLaunchable: Boolean
)

/** POST /api/device/apps body. */
data class ReportAppsRequest(
    @SerializedName("apps") val apps: List<DeviceAppItem>
)
