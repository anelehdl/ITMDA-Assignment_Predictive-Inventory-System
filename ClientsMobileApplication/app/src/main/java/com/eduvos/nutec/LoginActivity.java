package com.eduvos.nutec;

import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.ProgressBar;
import android.widget.Toast;

import androidx.appcompat.app.AppCompatActivity;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

import com.eduvos.nutec.pojo.LoginRequest;
import com.eduvos.nutec.pojo.LoginResponse;
import com.eduvos.nutec.api.RetrofitClient;
import com.eduvos.nutec.api.ApiService;
public class LoginActivity extends AppCompatActivity {

    private EditText emailInput;
    private EditText passwordInput;
    private Button loginButton;
    private ProgressBar progressBar;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_login);

        // Initialize views
        emailInput = findViewById(R.id.username);
        passwordInput = findViewById(R.id.password);
        loginButton = findViewById(R.id.login_button);
        progressBar = findViewById(R.id.login_progress_bar);

        // Check if user is already logged in
        String existingToken = RetrofitClient.loadAuthToken(this);
        if (existingToken != null && !existingToken.isEmpty()) {
            // User is already logged in, go to MainActivity
            navigateToMainActivity();
            return;
        }

        loginButton.setOnClickListener(v -> {
            String email = emailInput.getText().toString().trim();
            String password = passwordInput.getText().toString().trim();

            if (email.isEmpty() || password.isEmpty()) {
                Toast.makeText(this, "Please enter email and password", Toast.LENGTH_SHORT).show();
                return;
            }

            performLogin(email, password);
        });
    }

    private void performLogin(String email, String password) {
        // Show loading state
        setLoadingState(true);

        // Create login request with Email (not username!)
        LoginRequest loginRequest = new LoginRequest(email, password);

        // Make API call
        ApiService apiService = RetrofitClient.getApiService(this);
        Call<LoginResponse> call = apiService.login(loginRequest);

        call.enqueue(new Callback<LoginResponse>() {
            @Override
            public void onResponse(Call<LoginResponse> call, Response<LoginResponse> response) {
                setLoadingState(false);

                if (response.isSuccessful() && response.body() != null) {
                    LoginResponse loginResponse = response.body();

                    // Check if login was successful (your API returns a success field!)
                    if (loginResponse.isSuccess()) {
                        Log.d("LoginActivity", "Login successful for user: " + loginResponse.getFirstName());

                        // Save the authentication token
                        RetrofitClient.saveAuthToken(LoginActivity.this, loginResponse.getToken());

                        // Save user information
                        saveUserInfo(loginResponse);

                        // Show success message
                        Toast.makeText(LoginActivity.this,
                                "Welcome, " + loginResponse.getFirstName() + "!",
                                Toast.LENGTH_SHORT).show();

                        // Navigate to MainActivity
                        navigateToMainActivity();
                    } else {
                        // Login failed - your API returned success: false
                        Log.e("LoginActivity", "Login failed: " + loginResponse.getMessage());
                        Toast.makeText(LoginActivity.this,
                                loginResponse.getMessage(),
                                Toast.LENGTH_LONG).show();
                        emailInput.setError("Login failed");
                    }

                } else {
                    // HTTP error (not 200)
                    Log.e("LoginActivity", "Login failed with code: " + response.code());

                    String errorMessage = "Login failed. Please check your credentials.";
                    if (response.code() == 401) {
                        errorMessage = "Invalid email or password";
                    } else if (response.code() == 500) {
                        errorMessage = "Server error. Please try again later.";
                    }

                    Toast.makeText(LoginActivity.this, errorMessage, Toast.LENGTH_LONG).show();
                    emailInput.setError("Invalid credentials");
                }
            }

            @Override
            public void onFailure(Call<LoginResponse> call, Throwable t) {
                setLoadingState(false);

                Log.e("LoginActivity", "Login network error", t);
                Toast.makeText(LoginActivity.this,
                        "Network error: " + t.getMessage(),
                        Toast.LENGTH_LONG).show();
            }
        });
    }

    private void saveUserInfo(LoginResponse loginResponse) {
        SharedPreferences prefs = getSharedPreferences("MyAppPrefs", MODE_PRIVATE);
        SharedPreferences.Editor editor = prefs.edit();

        editor.putBoolean("loggedIn", true);
        editor.putString("userName", loginResponse.getFirstName()); // Using firstName as display name
        editor.putString("userEmail", loginResponse.getEmail());
        editor.putString("userId", loginResponse.getUserId());
        editor.putString("userRole", loginResponse.getRole());

        // Also save refresh token for future use
        if (loginResponse.getRefreshToken() != null) {
            editor.putString("refresh_token", loginResponse.getRefreshToken());
        }

        editor.apply();
    }

    private void navigateToMainActivity() {
        Intent intent = new Intent(LoginActivity.this, MainActivity.class);
        startActivity(intent);
        finish(); // Close LoginActivity so user can't go back
    }

    private void setLoadingState(boolean isLoading) {
        if (isLoading) {
            loginButton.setEnabled(false);
            loginButton.setText("Logging in...");
            emailInput.setEnabled(false);
            passwordInput.setEnabled(false);
            if (progressBar != null) {
                progressBar.setVisibility(View.VISIBLE);
            }
        } else {
            loginButton.setEnabled(true);
            loginButton.setText("Login");
            emailInput.setEnabled(true);
            passwordInput.setEnabled(true);
            if (progressBar != null) {
                progressBar.setVisibility(View.GONE);
            }
        }
    }
}
