plugins {
    alias(libs.plugins.android.application)
}

android {
    namespace = "com.eduvos.nutec"
    compileSdk = 36

    buildFeatures {
        dataBinding = true
    }

    defaultConfig {
        applicationId = "com.eduvos.nutec"
        minSdk = 24
        targetSdk = 36
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
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
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }


}

dependencies {

    //~~Database connection~~
    // Retrofit for networking
    implementation("com.squareup.retrofit2:retrofit:2.9.0")
    // Gson converter for JSON parsing
    implementation("com.squareup.retrofit2:converter-gson:2.9.0")
    // (Optional but recommended) OkHttp for logging network requests
    implementation("com.squareup.okhttp3:logging-interceptor:4.9.3")

    // RecyclerView for displaying lists
    implementation("androidx.recyclerview:recyclerview:1.3.2")

    //Cart dependancy
    implementation("androidx.cardview:cardview:1.0.0")

    //preference settings
    implementation("androidx.preference:preference:1.2.1")

    implementation(libs.appcompat)
    implementation(libs.material)
    implementation(libs.activity)
    implementation(libs.constraintlayout)

    testImplementation(libs.junit)
    androidTestImplementation(libs.ext.junit)
    androidTestImplementation(libs.espresso.core)

    implementation("androidx.fragment:fragment:1.8.1")
}