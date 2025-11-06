package com.eduvos.nutec.interceptor;

import android.content.Context;
import android.content.SharedPreferences;
import android.util.Log;
import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.Response;
import java.io.IOException;

public class AuthInterceptor implements Interceptor {
    private Context context;

    public AuthInterceptor(Context context) {
        this.context = context;
    }

    @Override
    public Response intercept(Chain chain) throws IOException {
        Request originalRequest = chain.request();

        // Get token from SharedPreferences
        String token = null;
        if (context != null) {
            SharedPreferences prefs = context.getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
            token = prefs.getString("auth_token", null);
        }

        // Log for debugging
        Log.d("AuthInterceptor", "Token: " + (token != null ? "exists" : "null"));

        // If no token, proceed without authorization
        if (token == null || token.isEmpty()) {
            return chain.proceed(originalRequest);
        }

        // Add Authorization header
        Request newRequest = originalRequest.newBuilder()
                .header("Authorization", "Bearer " + token)
                .build();

        Log.d("AuthInterceptor", "Added Authorization header");

        return chain.proceed(newRequest);
    }
}
