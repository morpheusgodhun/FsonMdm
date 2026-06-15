package com.fson.mdm.data.remote

import com.fson.mdm.data.remote.dto.AckRequest
import com.fson.mdm.data.remote.dto.CommandDto
import com.fson.mdm.data.remote.dto.HeartbeatRequest
import com.fson.mdm.data.remote.dto.LocationReportRequest
import com.fson.mdm.data.remote.dto.PolicyDto
import com.fson.mdm.data.remote.dto.RegisterRequest
import com.fson.mdm.data.remote.dto.RegisterResponse
import com.fson.mdm.data.remote.dto.ReportAppsRequest
import okhttp3.MultipartBody
import okhttp3.ResponseBody
import retrofit2.Response
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Header
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.Part
import retrofit2.http.Path
import retrofit2.http.Streaming
import retrofit2.http.Url

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

    // ---- Location ----
    @POST("api/device/location")
    suspend fun reportLocation(@Body body: LocationReportRequest): Response<Unit>

    // ---- Installed-app inventory ----
    @POST("api/device/apps")
    suspend fun reportApps(@Body body: ReportAppsRequest): Response<Unit>

    // ---- Remote screenshot upload (multipart, field name 'file') ----
    @Multipart
    @POST("api/device/screenshot")
    suspend fun uploadScreenshot(@Part file: MultipartBody.Part): Response<Unit>

    // ---- APK download (authorized device download; relative URL) ----
    @Streaming
    @GET
    suspend fun downloadFile(@Url url: String): Response<ResponseBody>
}
