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
import com.eduvos.nutec.adapter.OrderDetailItemsAdapter;
import com.eduvos.nutec.api.ApiService;
import com.eduvos.nutec.api.RetrofitClient;
import com.eduvos.nutec.pojo.Order;
import com.eduvos.nutec.pojo.OrderResponse;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.Locale;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

public class OrderDetailFragment extends Fragment {

    private static final String ARG_ORDER_ID = "order_id";

    private String orderId;
    private Order order;

    // UI Components
    private ProgressBar progressBar;
    private TextView orderNumberTextView;
    private TextView orderDateTextView;
    private TextView orderStatusTextView;
    private RecyclerView itemsRecyclerView;
    private TextView subtotalTextView;
    private TextView deliveryFeeTextView;
    private TextView taxAmountTextView;
    private TextView totalTextView;
    private OrderDetailItemsAdapter itemsAdapter;

    public static OrderDetailFragment newInstance(String orderId) {
        OrderDetailFragment fragment = new OrderDetailFragment();
        Bundle args = new Bundle();
        args.putString(ARG_ORDER_ID, orderId);
        fragment.setArguments(args);
        return fragment;
    }

    @Override
    public void onCreate(@Nullable Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        if (getArguments() != null) {
            orderId = getArguments().getString(ARG_ORDER_ID);
        }
    }

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container,
                             @Nullable Bundle savedInstanceState) {
        View view = inflater.inflate(R.layout.fragment_order_detail, container, false);

        initializeViews(view);
        setupRecyclerView();
        fetchOrderDetails();

        return view;
    }

    private void initializeViews(View view) {
        progressBar = view.findViewById(R.id.progress_bar);
        orderNumberTextView = view.findViewById(R.id.order_number);
        orderDateTextView = view.findViewById(R.id.order_date);
        orderStatusTextView = view.findViewById(R.id.order_status);
        itemsRecyclerView = view.findViewById(R.id.items_recycler_view);
        subtotalTextView = view.findViewById(R.id.subtotal);
        deliveryFeeTextView = view.findViewById(R.id.delivery_fee);
        taxAmountTextView = view.findViewById(R.id.tax_amount);
        totalTextView = view.findViewById(R.id.total);
    }

    private void setupRecyclerView() {
        itemsRecyclerView.setLayoutManager(new LinearLayoutManager(getContext()));
        itemsAdapter = new OrderDetailItemsAdapter();
        itemsRecyclerView.setAdapter(itemsAdapter);
    }

    private void fetchOrderDetails() {
        if (orderId == null) {
            Toast.makeText(getContext(), "Invalid order ID", Toast.LENGTH_SHORT).show();
            return;
        }

        progressBar.setVisibility(View.VISIBLE);

        ApiService apiService = RetrofitClient.getApiService(requireContext());
        Call<OrderResponse> call = apiService.getOrderById(orderId);

        call.enqueue(new Callback<OrderResponse>() {
            @Override
            public void onResponse(Call<OrderResponse> call, Response<OrderResponse> response) {
                if (!isAdded()) return;

                progressBar.setVisibility(View.GONE);

                if (response.isSuccessful() && response.body() != null) {
                    OrderResponse orderResponse = response.body();

                    if (orderResponse.isSuccess() && orderResponse.getOrder() != null) {
                        order = orderResponse.getOrder();
                        displayOrderDetails();
                    } else {
                        Toast.makeText(getContext(), "Order not found", Toast.LENGTH_SHORT).show();
                    }
                } else {
                    Toast.makeText(getContext(), "Failed to load order details", Toast.LENGTH_SHORT).show();
                    Log.e("OrderDetailFragment", "API Error: " + response.code());
                }
            }

            @Override
            public void onFailure(Call<OrderResponse> call, Throwable t) {
                if (!isAdded()) return;

                progressBar.setVisibility(View.GONE);
                Log.e("OrderDetailFragment", "Error fetching order details", t);
                Toast.makeText(getContext(), "Network error: " + t.getMessage(), Toast.LENGTH_SHORT).show();
            }
        });
    }

    private void displayOrderDetails() {
        if (order == null) return;

        // Set order header info
        orderNumberTextView.setText("Order #" + order.getOrderNumber());
        orderDateTextView.setText("Placed on " + formatDate(order.getCreatedAt()));
        orderStatusTextView.setText("Status: " + order.getStatus());

        // Set status color
        if ("Pending".equals(order.getStatus())) {
            orderStatusTextView.setTextColor(getResources().getColor(android.R.color.holo_orange_dark));
        } else if ("Completed".equals(order.getStatus())) {
            orderStatusTextView.setTextColor(getResources().getColor(android.R.color.holo_green_dark));
        } else if ("Cancelled".equals(order.getStatus())) {
            orderStatusTextView.setTextColor(getResources().getColor(android.R.color.holo_red_dark));
        }

        // Set items
        itemsAdapter.setItems(order.getItems());

        // Set price summary
        subtotalTextView.setText(String.format(Locale.getDefault(), "R%.2f", order.getSubtotal()));
        deliveryFeeTextView.setText(String.format(Locale.getDefault(), "R%.2f", order.getDeliveryFee()));
        taxAmountTextView.setText(String.format(Locale.getDefault(), "R%.2f", order.getTaxAmount()));
        totalTextView.setText(String.format(Locale.getDefault(), "R%.2f", order.getTotal()));
    }

    private String formatDate(String dateString) {
        try {
            SimpleDateFormat inputFormat = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault());
            SimpleDateFormat outputFormat = new SimpleDateFormat("MMMM dd, yyyy 'at' hh:mm a", Locale.getDefault());
            Date date = inputFormat.parse(dateString);
            return outputFormat.format(date);
        } catch (ParseException e) {
            return dateString;
        }
    }
}