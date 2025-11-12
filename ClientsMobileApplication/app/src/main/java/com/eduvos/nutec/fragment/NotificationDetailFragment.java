package com.eduvos.nutec.fragment;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.widget.Toolbar;
import androidx.fragment.app.Fragment;

import com.eduvos.nutec.pojo.AppNotification;
import com.eduvos.nutec.manager.NotificationManager;
import com.eduvos.nutec.R;
import com.google.android.material.bottomnavigation.BottomNavigationView;
import java.util.List;

public class NotificationDetailFragment extends Fragment {

    private List<AppNotification> notifications;
    private int currentPosition;

    private TextView detailTitle, detailTimestamp, detailBody;
    private Button buttonPrevious, buttonNext;

    @Override
    public void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        notifications = NotificationManager.getInstance().getNotifications();
        if (getArguments() != null) {
            currentPosition = getArguments().getInt("notification_position", 0);
        }
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        toggleMainUI(View.GONE); // Hide main activity UI
        View view = inflater.inflate(R.layout.fragment_notification_detail, container, false);

        // Setup this fragment's own toolbar
        Toolbar detailToolbar = view.findViewById(R.id.detail_toolbar);
        detailToolbar.setNavigationIcon(R.drawable.ic_arrow_back); // Make sure you have this icon
        detailToolbar.setNavigationOnClickListener(v -> {
            // This will perform the "back" action, returning to the notification list
            requireActivity().getSupportFragmentManager().popBackStack();
        });

        //link views
        detailTitle = view.findViewById(R.id.detail_title);
        detailTimestamp = view.findViewById(R.id.detail_timestamp);
        detailBody = view.findViewById(R.id.detail_body);
        buttonPrevious = view.findViewById(R.id.button_previous);
        buttonNext = view.findViewById(R.id.button_next);

        buttonPrevious.setOnClickListener(v -> showPreviousNotification());
        buttonNext.setOnClickListener(v -> showNextNotification());

        displayNotification(currentPosition);
        return view;
    }

    @Override
    public void onDestroyView() {
        super.onDestroyView();
        toggleMainUI(View.VISIBLE); // Restore main activity UI
    }

    private void displayNotification(int position) {
        AppNotification notification = notifications.get(position);
        detailTitle.setText(notification.getTitle());
        detailTimestamp.setText(notification.getFormattedTimestamp());
        detailBody.setText(notification.getDetails());

        notification.setRead(true); // Mark as read

        buttonPrevious.setEnabled(position > 0);
        buttonNext.setEnabled(position < notifications.size() - 1);
    }

    private void showPreviousNotification() {
        if (currentPosition > 0) {
            currentPosition--;
            displayNotification(currentPosition);
        }
    }

    private void showNextNotification() {
        if (currentPosition < notifications.size() - 1) {
            currentPosition++;
            displayNotification(currentPosition);
        }
    }

    private void toggleMainUI(int visibility) {
        if (getActivity() == null) return;
        View header = getActivity().findViewById(R.id.header_layout);
        BottomNavigationView bottomNav = getActivity().findViewById(R.id.bottom_navigation);
        if (header != null) header.setVisibility(visibility);
        if (bottomNav != null) bottomNav.setVisibility(visibility);
    }
}
