# Retrofit / OkHttp / Gson keep rules
-keepattributes Signature
-keepattributes *Annotation*
-keep class com.fson.mdm.data.remote.dto.** { *; }
-keepclassmembers,allowobfuscation class * {
    @com.google.gson.annotations.SerializedName <fields>;
}
-dontwarn okhttp3.**
-dontwarn retrofit2.**
