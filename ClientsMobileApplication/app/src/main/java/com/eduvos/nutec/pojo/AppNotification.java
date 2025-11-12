//AppNotification.java
//
//        Group Members: Charlene Higgo, Armand Geldenhuys, Aneleh de Lange, Travis Musson, Grant Peterson, Petrus
//        2025
//        Developed for NuTec Inks

package com.eduvos.nutec.pojo;

import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

public class AppNotification {
    private String title;
    private String details;
    private long timestamp;
    private boolean isRead;
    private boolean wasSuccess;

    public AppNotification(String title, String details, boolean wasSuccess) {
        this.title = title;
        this.details = details;
        this.timestamp = System.currentTimeMillis();
        this.wasSuccess = wasSuccess;
        this.isRead = false; // All new notifications start as unread
    }

    // --- Getters ---
    public String getTitle() {
        return title; }
    public String getDetails() {
        return details; }
    public boolean wasSuccess() {
        return wasSuccess; }
    public boolean isRead() {
        return isRead; }

    public String getFormattedTimestamp() {
        SimpleDateFormat sdf = new SimpleDateFormat("dd MMM yyyy, HH:mm", Locale.getDefault());
        return sdf.format(new Date(timestamp));
    }

    // --- Setter ---
    public void setRead(boolean read) { isRead = read; }
}

