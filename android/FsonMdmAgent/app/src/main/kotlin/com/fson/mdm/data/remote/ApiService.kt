package com.fson.mdm.data.remote

import com.fson.mdm.data.remote.dto.AckRequest
import com.fson.mdm.data.remote.dto.CommandDto
import com.fson.mdm.data.remote.dto.HeartbeatRequest
import com.fson.mdm.data.remote.dto.PolicyDto
import com.fson.mdm.data.remote.dto.RegisterRequest
import com.fson.mdm.data.remote.dto.RegisterResponse
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.POST
import retrofit2.http.Path

/**
 * Retrofit definition of the FSON MDM backend. Endpoint paths and the JSON
 * contract mirror the ASP.NET Core controllers exactly.
 */
interface ApiService {

    @POST("api/device/register")
    suspend fun register(
        @Header("X-Enrollment-Token") enrollmentToken: String,
        @Body body: RegisterRequest
    ): Response<RegisterResponse>

    @POST("api/device/heartbeat")
    suspend fun heartbeat(@Body body: HeartbeatRequest): Response<Unit>

    @GET("api/policy/{deviceId}")
    suspend fun getPolicy(@Path("deviceId") deviceId: String): Response<PolicyDto>

    @GET("api/command/pending/{deviceId}")
    suspend fun pendingCommands(@Path("deviceId") deviceId: String): Response<List<CommandDto>>

    @POST("api/command/ack")
    suspend fun ackCommand(@Body body: AckRequest): Response<Unit>
}
