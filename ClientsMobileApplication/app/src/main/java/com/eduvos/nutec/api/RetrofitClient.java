package com.eduvos.nutec.api;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;
import android.widget.Toast;
import okhttp3.OkHttpClient;
import okhttp3.logging.HttpLoggingInterceptor;
import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;
import retrofit2.Retrofit;
import retrofit2.converter.gson.GsonConverterFactory;
import com.google.gson.Gson;
import com.google.gson.GsonBuilder;
import com.eduvos.nutec.interceptor.AuthInterceptor;

public class RetrofitClient {

    private static final String BASE_URL = "http://10.0.2.2:5000/api/";  // Changed: removed /api/

    private static Retrofit retrofit = null;
    private static Context appContext = null;

    /**
     * Initialize RetrofitClient with application context
     * Call this once in your Application class or before first use
     */
    public static void initialize(Context context) {
        appContext = context.getApplicationContext();
    }

    public static ApiService getApiService(Context context) {
        if (appContext == null) {
            initialize(context);
        }

        if (retrofit == null) {
            // Create the auth interceptor with context
            AuthInterceptor authInterceptor = new AuthInterceptor(appContext);

            // Create a logging interceptor to see request and response logs
            HttpLoggingInterceptor loggingInterceptor = new HttpLoggingInterceptor();
            loggingInterceptor.setLevel(HttpLoggingInterceptor.Level.BODY);

            // Create an OkHttpClient and add the interceptors
            OkHttpClient client = new OkHttpClient.Builder()
                    .addInterceptor(authInterceptor) // Add auth interceptor FIRST
                    .addInterceptor(loggingInterceptor) // Then logging interceptor
                    .build();

            // Create a Gson instance that is lenient with JSON parsing
            Gson gson = new GsonBuilder()
                    .setLenient()
                    .create();

            retrofit = new Retrofit.Builder()
                    .baseUrl(BASE_URL)
                    .client(client) // Set the custom client
                    .addConverterFactory(GsonConverterFactory.create(gson))
                    .build();
        }
        return retrofit.create(ApiService.class);
    }

    /**
     * Save token to SharedPreferences for persistence across app restarts
     */
    public static void saveAuthToken(Context context, String token) {
        SharedPreferences prefs = context.getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
        prefs.edit().putString("auth_token", token).apply();
        Log.d("RetrofitClient", "Token saved to SharedPreferences");
    }

    /**
     * Load token from SharedPreferences
     */
    public static String loadAuthToken(Context context) {
        SharedPreferences prefs = context.getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
        String token = prefs.getString("auth_token", null);
        Log.d("RetrofitClient", "Token loaded: " + (token != null ? "exists" : "null"));
        return token;
    }

    /**
     * Clear the authentication token (useful for logout)
     */
    public static void clearAuthToken(Context context) {
        SharedPreferences prefs = context.getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
        prefs.edit().remove("auth_token").apply();
        Log.d("RetrofitClient", "Token cleared from SharedPreferences");
    }

    // Test connection method
    public void pingServer(Context context) {
        ApiService apiService = RetrofitClient.getApiService(context);
        Log.d("API_TEST", "About to call ping endpoint");
        apiService.ping().enqueue(new Callback<PingResponse>() {
            @Override
            public void onResponse(Call<PingResponse> call, Response<PingResponse> response) {
                Log.d("API_TEST", "Response received! Code: " + response.code());
                if (response.isSuccessful() && response.body() != null) {
                    PingResponse ping = response.body();
                    Log.d("API", "API is working: " + ping.getMessage());
                    Toast.makeText(context,
                            "Connected! " + ping.getMessage(),
                            Toast.LENGTH_SHORT).show();
                } else {
                    Log.d("API_TEST", "SUCCESS (but maybe no body)!");
                }
            }

            @Override
            public void onFailure(Call<PingResponse> call, Throwable t) {
                Log.e("API", "Connection failed: " + t.getMessage());
                Log.e("API_TEST", "FAILED: " + t.getMessage());
                Toast.makeText(context,
                        "Failed: " + t.getMessage(),
                        Toast.LENGTH_SHORT).show();
            }
        });
    }
}
