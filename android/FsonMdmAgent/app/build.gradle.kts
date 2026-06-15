plugins {
    id("com.android.application")
    id("org.jetbrains.kotlin.android")
}

android {
    namespace = "com.fson.mdm"
    compileSdk = 34

    defaultConfig {
        applicationId = "com.fson.mdm"
        minSdk = 26
        targetSdk = 34
        versionCode = 1
        versionName = "1.0.0"

        // Default backend base URL. 10.0.2.2 is the host loopback as seen from the
        // Android emulator. Change in MainActivity / Ayarlar for a physical device.
        buildConfigField("String", "DEFAULT_BASE_URL", "\"http://10.0.2.2:5080/\"")
        buildConfigField("String", "DEFAULT_ENROLLMENT_KEY", "\"FSON-DEMO-ENROLLMENT-KEY\"")
    }

    buildTypes {
        release {
            isMinifyEnabled = false
            proguardFiles(
                getDefaultProguardFile("proguard-android-optimize.txt"),
                "proguard-rules.pro"
            )
        }
    }

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
    }

    kotlinOptions {
        jvmTarget = "17"
    }

    buildFeatures {
        viewBinding = true
        buildConfig = true
    }

    // Kotlin sources live under src/main/kotlin
    sourceSets["main"].java.srcDirs("src/main/kotlin")
}

dependencies {
    // AndroidX core / UI
    implementation("androidx.core:core-ktx:1.13.1")
    implementation("androidx.appcompat:appcompat:1.7.0")
    implementation("com.google.android.material:material:1.12.0")
    implementation("androidx.constraintlayout:constraintlayout:2.1.4")
    implementation("androidx.activity:activity-ktx:1.9.1")

    // Lifecycle + Coroutines
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-android:1.8.1")
    implementation("androidx.lifecycle:lifecycle-runtime-ktx:2.8.4")
    implementation("androidx.lifecycle:lifecycle-service:2.8.4")

    // WorkManager (heartbeat + policy polling)
    implementation("androidx.work:work-runtime-ktx:2.9.1")

    // Retrofit + OkHttp + Gson (REST client)
    implementation("com.squareup.retrofit2:retrofit:2.11.0")
    implementation("com.squareup.retrofit2:converter-gson:2.11.0")
    implementation("com.squareup.okhttp3:okhttp:4.12.0")
    implementation("com.squareup.okhttp3:logging-interceptor:4.12.0")
    implementation("com.google.code.gson:gson:2.11.0")
}
