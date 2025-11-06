package com.eduvos.nutec.adapter;

import android.view.LayoutInflater;import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.pojo.AppNotification;
import com.eduvos.nutec.R;

import java.util.List;

public class NotificationsAdapter extends RecyclerView.Adapter<NotificationsAdapter.NotificationViewHolder> {

    private final List<AppNotification> notifications;
    private final OnNotificationClickListener listener;

    // Interface to handle clicks on each notification item
    public interface OnNotificationClickListener {
        void onNotificationClicked(AppNotification notification, int position);
    }

    public NotificationsAdapter(List<AppNotification> notifications, OnNotificationClickListener listener) {
        this.notifications = notifications;
        this.listener = listener;
    }

    @NonNull
    @Override
    public NotificationViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        // Inflate the layout for a single notification item
        View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_notification, parent, false);
        return new NotificationViewHolder(view);
    }

    // This is the method where the "Unexpected token" error was occurring.
    // The code now correctly sits inside this method.
    @Override
    public void onBindViewHolder(@NonNull NotificationViewHolder holder, int position) {
        AppNotification notification = notifications.get(position);

        holder.title.setText(notification.getTitle());
        holder.timestamp.setText(notification.getFormattedTimestamp());

        // Show or hide the blue "unread" dot
        holder.unreadIndicator.setVisibility(notification.isRead() ? View.GONE : View.VISIBLE);

        // Set the icon to a checkmark for success or an error icon for failure
        holder.icon.setImageResource(notification.wasSuccess() ? R.drawable.ic_success : R.drawable.ic_error);

        // Set a click listener on the entire item view
        holder.itemView.setOnClickListener(v -> {
            if (listener != null) {
                // Mark the notification as read and pass the click event to the fragment
                notification.setRead(true);
                listener.onNotificationClicked(notification, holder.getAdapterPosition());
                // Instantly hide the "unread" dot
                notifyItemChanged(holder.getAdapterPosition());
            }
        });
    }

    @Override
    public int getItemCount() {
        return notifications.size();
    }

    // ViewHolder class to hold references to the views in item_notification.xml
    public static class NotificationViewHolder extends RecyclerView.ViewHolder {
        ImageView icon;
        TextView title;
        TextView timestamp;
        ImageView unreadIndicator;

        public NotificationViewHolder(@NonNull View itemView) {
            super(itemView);
            icon = itemView.findViewById(R.id.notification_icon);
            title = itemView.findViewById(R.id.notification_title);
            timestamp = itemView.findViewById(R.id.notification_timestamp);
            unreadIndicator = itemView.findViewById(R.id.unread_indicator);
        }
    }
}
