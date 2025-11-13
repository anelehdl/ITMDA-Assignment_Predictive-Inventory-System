package com.eduvos.nutec.api;

public class PingResponse {
    private String message;
    private String timestamp;

    public String getMessage() { return message; }
    public void setMessage(String message) { this.message = message; }

    public String getTimestamp() { return timestamp; }
    public void setTimestamp(String timestamp) { this.timestamp = timestamp; }
}