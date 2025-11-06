package com.eduvos.nutec.manager;

import androidx.lifecycle.LiveData;
import androidx.lifecycle.MutableLiveData;

import com.eduvos.nutec.pojo.AppNotification;

import java.util.ArrayList;
import java.util.List;

public class NotificationManager {

    private static NotificationManager instance;
    private final List<AppNotification> notifications = new ArrayList<>();

    // LiveData to automatically update the UI when the unread count changes
    private final MutableLiveData<Integer> unreadCount = new MutableLiveData<>(0);

    private NotificationManager() {}

    public static synchronized NotificationManager getInstance() {
        if (instance == null) {
            instance = new NotificationManager();
        }
        return instance;
    }

    public void addNotification(AppNotification notification) {
        notifications.add(0, notification); // Add new notifications to the top
        updateUnreadCount();
    }

    public List<AppNotification> getNotifications() {
        return notifications;
    }

    public LiveData<Integer> getUnreadCount() {
        return unreadCount;
    }

    public void markAllAsRead() {
        for (AppNotification notification : notifications) {
            notification.setRead(true);
        }
        updateUnreadCount();
    }

    private void updateUnreadCount() {
        int count = 0;
        for (AppNotification notification : notifications) {
            if (!notification.isRead()) {
                count++;
            }
        }
        unreadCount.postValue(count); // Use postValue for thread-safety
    }
}
