package com.eduvos.nutec.fragment;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.DividerItemDecoration;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.pojo.AppNotification;
import com.eduvos.nutec.manager.NotificationManager;
import com.eduvos.nutec.adapter.NotificationsAdapter;
import com.eduvos.nutec.R;

import java.util.List;

public class NotificationsFragment extends Fragment implements NotificationsAdapter.OnNotificationClickListener {

    // Declare RecyclerView and Adapter as member variables
    private RecyclerView recyclerView;
    private NotificationsAdapter adapter;
    private List<AppNotification> notifications;
    private TextView emptyMessageView;

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        // Inflate your generic list layout
        View view = inflater.inflate(R.layout.fragment_list_view, container, false);

        // Initialize the views from the layout
        recyclerView = view.findViewById(R.id.list_recycler_view);
        emptyMessageView = view.findViewById(R.id.empty_list_message);

        // Set the empty message specific to this screen
        emptyMessageView.setText("You have no notifications.");

        // Get the list of notifications from the manager
        notifications = NotificationManager.getInstance().getNotifications();

        // Set up the RecyclerView
        recyclerView.setLayoutManager(new LinearLayoutManager(getContext()));
        // Add a divider line between items for better readability
        recyclerView.addItemDecoration(new DividerItemDecoration(requireContext(), DividerItemDecoration.VERTICAL));

        // Initialize the adapter with the data and the click listener (this fragment)
        adapter = new NotificationsAdapter(notifications, this);

        // Set the adapter on the RecyclerView
        recyclerView.setAdapter(adapter);

        // Mark all notifications as read as soon as the user opens this screen
        NotificationManager.getInstance().markAllAsRead();

        // Check if the list is empty and show the message if it is
        updateEmptyView();

        return view;
    }

    private void updateEmptyView() {
        if (notifications.isEmpty()) {
            recyclerView.setVisibility(View.GONE);
            emptyMessageView.setVisibility(View.VISIBLE);
        } else {
            recyclerView.setVisibility(View.VISIBLE);
            emptyMessageView.setVisibility(View.GONE);
        }
    }

    @Override
    public void onResume() {
        super.onResume();
        // Refresh the view when the user returns to this fragment
        if (adapter != null) {
            adapter.notifyDataSetChanged();
            updateEmptyView();
        }
    }

    @Override
    public void onNotificationClicked(AppNotification notification, int position) {
        // Navigate to a NotificationDetailFragment when an item is clicked
        Fragment detailFragment = new NotificationDetailFragment();

        // Use a Bundle to pass the position of the clicked notification
        Bundle args = new Bundle();
        args.putInt("notification_position", position);
        detailFragment.setArguments(args);

        // Perform the fragment transaction
        requireActivity().getSupportFragmentManager()
                .beginTransaction()
                .replace(R.id.frame_layout, detailFragment)
                .addToBackStack(null) // Allows the user to return to the list
                .commit();
    }
}

