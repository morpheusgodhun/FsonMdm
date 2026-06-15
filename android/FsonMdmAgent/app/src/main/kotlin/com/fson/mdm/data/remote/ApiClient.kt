package com.fson.mdm.data.remote

import com.fson.mdm.core.Prefs
import okhttp3.OkHttpClient
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

/**
 * Builds and caches the Retrofit-backed [ApiService]. The cache is keyed by
 * base URL so changing the server address in the UI transparently rebuilds it.
 */
object ApiClient {

    @Volatile private var cached: ApiService? = null
    @Volatile private var cachedBaseUrl: String? = null

    fun service(prefs: Prefs): ApiService {
        val baseUrl = Prefs.normalizeUrl(prefs.baseUrl)
        val current = cached
        if (current != null && cachedBaseUrl == baseUrl) return current

        return synchronized(this) {
            val again = cached
            if (again != null && cachedBaseUrl == baseUrl) return again

            val logging = HttpLoggingInterceptor().apply {
                level = HttpLoggingInterceptor.Level.BASIC
            }

            val ok = OkHttpClient.Builder()
                .addInterceptor(AuthInterceptor(prefs))
                .addInterceptor(logging)
                .connectTimeout(15, TimeUnit.SECONDS)
                .readTimeout(20, TimeUnit.SECONDS)
                .writeTimeout(20, TimeUnit.SECONDS)
                .build()

            val retrofit = Retrofit.Builder()
                .baseUrl(baseUrl)
                .client(ok)
                .addConverterFactory(GsonConverterFactory.create())
                .build()

            retrofit.create(ApiService::class.java).also {
                cached = it
                cachedBaseUrl = baseUrl
            }
        }
    }

    /** Force a rebuild on next [service] call (e.g. after base URL edits). */
    fun invalidate() {
        cached = null
        cachedBaseUrl = null
    }
}
