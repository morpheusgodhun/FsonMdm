package com.fson.mdm.data.remote

import com.fson.mdm.core.Prefs
import okhttp3.Interceptor
import okhttp3.Response

/**
 * Attaches the device JWT as a Bearer token to every request except the
 * register call (which authenticates with the enrollment header instead).
 */
class AuthInterceptor(private val prefs: Prefs) : Interceptor {

    override fun intercept(chain: Interceptor.Chain): Response {
        val original = chain.request()

        // Register endpoint must not carry a (possibly stale) bearer token.
        if (original.url.encodedPath.endsWith("/api/device/register")) {
            return chain.proceed(original)
        }

        val token = prefs.token
        val request = if (!token.isNullOrBlank()) {
            original.newBuilder()
                .header("Authorization", "Bearer $token")
                .build()
        } else {
            original
        }
        return chain.proceed(request)
    }
}
