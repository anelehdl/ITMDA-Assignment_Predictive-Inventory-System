package com.eduvos.nutec.fragment;

import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ProgressBar;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.R;
import com.eduvos.nutec.adapter.OrdersAdapter;
import com.eduvos.nutec.api.ApiService;
import com.eduvos.nutec.api.RetrofitClient;
import com.eduvos.nutec.pojo.Order;
import com.eduvos.nutec.pojo.OrdersListResponse;

import java.util.ArrayList;
import java.util.List;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class OrdersFragment extends Fragment {

    private RecyclerView recyclerView;
    private OrdersAdapter adapter;
    private ProgressBar progressBar;
    private TextView emptyMessageView;
    private List<Order> ordersList = new ArrayList<>();

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        View view = inflater.inflate(R.layout.fragment_orders, container, false);

        initializeViews(view);
        setupRecyclerView();
        fetchOrders();

        return view;
    }

    private void initializeViews(View view) {
        recyclerView = view.findViewById(R.id.orders_recycler_view);
        progressBar = view.findViewById(R.id.progress_bar);
        emptyMessageView = view.findViewById(R.id.empty_message);
    }

    private void setupRecyclerView() {
        recyclerView.setLayoutManager(new LinearLayoutManager(getContext()));
        adapter = new OrdersAdapter(ordersList, order -> {
            // Handle order click - navigate to order details
            navigateToOrderDetails(order);
        });
        recyclerView.setAdapter(adapter);
    }

    private void fetchOrders() {
        progressBar.setVisibility(View.VISIBLE);
        recyclerView.setVisibility(View.GONE);
        emptyMessageView.setVisibility(View.GONE);

        ApiService apiService = RetrofitClient.getApiService(requireContext());
        Call<OrdersListResponse> call = apiService.getUserOrders();

        call.enqueue(new Callback<OrdersListResponse>() {
            @Override
            public void onResponse(Call<OrdersListResponse> call, Response<OrdersListResponse> response) {
                if (!isAdded()) return;

                progressBar.setVisibility(View.GONE);

                if (response.isSuccessful() && response.body() != null) {
                    OrdersListResponse ordersResponse = response.body();

                    if (ordersResponse.isSuccess() && ordersResponse.getOrders() != null) {
                        ordersList.clear();
                        ordersList.addAll(ordersResponse.getOrders());
                        adapter.notifyDataSetChanged();

                        updateEmptyView();
                    } else {
                        showEmptyView("No orders found");
                    }
                } else {
                    Toast.makeText(getContext(), "Failed to load orders", Toast.LENGTH_SHORT).show();
                    showEmptyView("Failed to load orders");
                }
            }

            @Override
            public void onFailure(Call<OrdersListResponse> call, Throwable t) {
                if (!isAdded()) return;

                progressBar.setVisibility(View.GONE);
                Log.e("OrdersFragment", "Error fetching orders", t);
                Toast.makeText(getContext(), "Network error: " + t.getMessage(), Toast.LENGTH_SHORT).show();
                showEmptyView("Network error");
            }
        });
    }

    private void updateEmptyView() {
        if (ordersList.isEmpty()) {
            showEmptyView("You haven't placed any orders yet");
        } else {
            recyclerView.setVisibility(View.VISIBLE);
            emptyMessageView.setVisibility(View.GONE);
        }
    }

    private void showEmptyView(String message) {
        recyclerView.setVisibility(View.GONE);
        emptyMessageView.setVisibility(View.VISIBLE);
        emptyMessageView.setText(message);
    }

    private void navigateToOrderDetails(Order order) {
        // Navigate to order details fragment
        OrderDetailFragment fragment = OrderDetailFragment.newInstance(order.getId());
        requireActivity().getSupportFragmentManager().beginTransaction()
                .replace(R.id.frame_layout, fragment)
                .addToBackStack(null)
                .commit();
    }
}