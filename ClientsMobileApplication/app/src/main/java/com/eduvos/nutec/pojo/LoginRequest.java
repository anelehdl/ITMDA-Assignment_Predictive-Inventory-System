package com.eduvos.nutec.pojo;

import com.google.gson.annotations.SerializedName;

/**
 * Login request matching Core.Models.DTO.LoginRequestDto
 * Your API expects: Email and Password
 */
public class LoginRequest {
    @SerializedName("email")
    private String email;

    @SerializedName("password")
    private String password;

    public LoginRequest(String email, String password) {
        this.email = email;
        this.password = password;
    }

    public String getEmail() {
        return email;
    }

    public String getPassword() {
        return password;
    }
}
