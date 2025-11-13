package com.eduvos.nutec.interceptor;

import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.util.Base64;
import android.util.Log;

import com.eduvos.nutec.LoginActivity;

import org.json.JSONObject;

import okhttp3.Interceptor;
import okhttp3.Request;
import okhttp3.Response;
import okhttp3.MediaType;
import okhttp3.RequestBody;
import okhttp3.OkHttpClient;

import java.io.IOException;

public class AuthInterceptor implements Interceptor {
    private static final String TAG = "AuthInterceptor";
    private Context context;

    public AuthInterceptor(Context context) {
        this.context = context;
    }

    @Override
    public Response intercept(Chain chain) throws IOException {
        Request originalRequest = chain.request();

        String url = originalRequest.url().toString();
        if (url.contains("/api/auth/login") ||
                url.contains("/api/auth/refresh") ||
                url.contains("/api/auth/logout")) {
            Log.d(TAG, "Skipping auth interceptor for auth endpoint: " + url);
            return chain.proceed(originalRequest);
        }

        // Check if token is expired before making the request
        if (isTokenExpired()) {
            Log.d(TAG, "Token expired, attempting refresh");
            String newToken = refreshToken();

            if (newToken == null) {
                // Refresh failed, redirect to login
                clearTokenAndRedirectToLogin();
                throw new IOException("Token refresh failed");
            }
        }

        // Add token to request
        Request newRequest = addTokenToRequest(originalRequest);
        Response response = chain.proceed(newRequest);

        // If we still get 401, try to refresh the token one more time
        if (response.code() == 401) {
            Log.d(TAG, "Received 401, attempting token refresh");
            synchronized (this) {
                response.close();

                String refreshedToken = refreshToken();

                if (refreshedToken != null) {
                    // Retry the request with the new token
                    Request retryRequest = addTokenToRequest(originalRequest);
                    return chain.proceed(retryRequest);
                } else {
                    // Refresh failed, redirect to login
                    clearTokenAndRedirectToLogin();
                    throw new IOException("Token refresh failed");
                }
            }
        }

        return response;
    }

    private Request addTokenToRequest(Request originalRequest) {
        String token = getToken();

        if (token == null || token.isEmpty()) {
            return originalRequest;
        }

        Log.d(TAG, "Adding Authorization header");
        return originalRequest.newBuilder()
                .header("Authorization", "Bearer " + token)
                .build();
    }

    private String getToken() {
        if (context == null) return null;

        SharedPreferences prefs = context.getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
        return prefs.getString("auth_token", null);
    }

    private String getRefreshToken() {
        if (context == null) {
            Log.e(TAG, "Context is null in getRefreshToken");
            return null;
        }

        SharedPreferences prefs = context.getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
        String token = prefs.getString("refresh_token", null);

        Log.d(TAG, "=== GET REFRESH TOKEN DEBUG ===");
        Log.d(TAG, "SharedPreferences has refresh_token key: " + prefs.contains("refresh_token"));
        Log.d(TAG, "Token is null: " + (token == null));
        if (token != null) {
            Log.d(TAG, "Token length: " + token.length());
            Log.d(TAG, "Token first 10 chars: " + token.substring(0, Math.min(10, token.length())));
        }
        Log.d(TAG, "=== END GET REFRESH TOKEN DEBUG ===");

        return token;
    }

    private boolean isTokenExpired() {
        String token = getToken();
        if (token == null) return true;

        try {
            // Decode JWT token to get expiration
            String[] parts = token.split("\\.");
            if (parts.length < 2) return true;

            String payload = new String(Base64.decode(parts[1], Base64.URL_SAFE | Base64.NO_WRAP));
            JSONObject json = new JSONObject(payload);
            long exp = json.getLong("exp");
            long currentTime = System.currentTimeMillis() / 1000;

            // Check if token expires in the next 60 seconds
            boolean expired = currentTime >= (exp - 60);
            Log.d(TAG, "Token expired: " + expired);
            return expired;
        } catch (Exception e) {
            Log.e(TAG, "Error checking token expiration", e);
            return true;
        }
    }

    private String refreshToken() {
        String refreshToken = getRefreshToken();

        if (refreshToken == null || refreshToken.isEmpty()) {
            Log.e(TAG, "No refresh token available");
            return null;
        }

        try {
            // Create a new OkHttpClient without this interceptor to avoid infinite loop
            OkHttpClient client = new OkHttpClient.Builder().build();

            // Create refresh token request
            JSONObject jsonBody = new JSONObject();
            jsonBody.put("RefreshToken", refreshToken);

            RequestBody body = RequestBody.create(
                    jsonBody.toString(),
                    MediaType.parse("application/json")
            );

            Request request = new Request.Builder()
                    .url("http://10.0.2.2:5000/api/auth/refresh") // Update with your actual refresh endpoint
                    .post(body)
                    .build();

            Response response = client.newCall(request).execute();

            if (response.isSuccessful() && response.body() != null) {
                String responseBody = response.body().string();
                Log.d(TAG, "Refresh response: " + responseBody);

                JSONObject jsonResponse = new JSONObject(responseBody);

                // Check if refresh was successful
                boolean success = jsonResponse.optBoolean("Success", false);
                if (!success) {
                    Log.e(TAG, "Token refresh failed: " + jsonResponse.optString("Message"));
                    return null;
                }

                String newToken = jsonResponse.getString("Token");
                String newRefreshToken = jsonResponse.optString("RefreshToken", refreshToken);

                // Save new tokens
                SharedPreferences prefs = context.getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
                prefs.edit()
                        .putString("auth_token", newToken)
                        .putString("refresh_token", newRefreshToken)
                        .apply();

                Log.d(TAG, "Token refreshed successfully");
                return newToken;
            } else {
                Log.e(TAG, "Token refresh failed with code: " + response.code());
                if (response.body() != null) {
                    Log.e(TAG, "Error body: " + response.body().string());
                }
                return null;
            }
        } catch (Exception e) {
            Log.e(TAG, "Error refreshing token", e);
            return null;
        }
    }

    private void clearTokenAndRedirectToLogin() {
        if (context == null) return;

        Log.d(TAG, "Clearing tokens and redirecting to login");

        SharedPreferences prefs = context.getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
        prefs.edit()
                .remove("auth_token")
                .remove("refresh_token")
                .apply();

        Intent intent = new Intent(context, LoginActivity.class);
        intent.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        context.startActivity(intent);
    }
}