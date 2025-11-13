package com.eduvos.nutec.fragment;

import android.os.Bundle;
import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.app.AlertDialog;
import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

import com.eduvos.nutec.adapter.CartAdapter;
import com.eduvos.nutec.R;
import com.eduvos.nutec.pojo.CartItem;
import com.eduvos.nutec.pojo.CreateOrderRequest;
import com.eduvos.nutec.pojo.OrderResponse;
import com.eduvos.nutec.pojo.OrderItem;
import com.eduvos.nutec.api.ApiService;
import com.eduvos.nutec.api.RetrofitClient;
import com.eduvos.nutec.manager.CartManager;
import com.eduvos.nutec.manager.NotificationManager;
import com.eduvos.nutec.pojo.AppNotification;

public class CartFragment extends Fragment implements CartAdapter.OnCartItemChangedListener {

    private RecyclerView recyclerView;
    private CartAdapter adapter;
    private List<CartItem> cartItems;

    private TextView subtotalTextView, deliveryTextView, taxesTextView, totalTextView, emptyCartMessage;
    private Button checkoutButton;

    private static final double DELIVERY_FEE = 50.00;
    private static final double TAX_RATE = 0.15; // 15% VAT

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        View view = inflater.inflate(R.layout.fragment_cart, container, false);

        // Initialize views
        recyclerView = view.findViewById(R.id.cart_recycler_view);
        subtotalTextView = view.findViewById(R.id.summary_subtotal);
        deliveryTextView = view.findViewById(R.id.summary_delivery);
        taxesTextView = view.findViewById(R.id.summary_taxes);
        totalTextView = view.findViewById(R.id.summary_total);
        emptyCartMessage = view.findViewById(R.id.empty_cart_message);
        checkoutButton = view.findViewById(R.id.button_checkout);

        return view;
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        // Setup RecyclerView
        recyclerView.setLayoutManager(new LinearLayoutManager(getContext()));
        // --- Get the items LocalDate.from the CartManager ---
        this.cartItems = CartManager.getInstance().getCartItems();
        adapter = new CartAdapter(cartItems, this); // 'this' refers to the fragment implementing the listener
        recyclerView.setAdapter(adapter);

        // Setup checkout button
        checkoutButton.setOnClickListener(v -> {
            if (cartItems.isEmpty()) {
                Toast.makeText(getContext(), "Your cart is empty.", Toast.LENGTH_SHORT).show();
            } else {
                // Proceed to checkout logic
                Toast.makeText(getContext(), "Proceeding to checkout...", Toast.LENGTH_SHORT).show();
                submitOrderToApi();
            }
        });

        // Initial UI update
        updatePriceSummary();
    }

    private void submitOrderToApi() {
        // Build the order items from cart data with the new structure
        List<OrderItem> orderItems = new ArrayList<>();
        double subtotal = 0;

        for (CartItem item : cartItems) {
            double itemTotal = item.getPrice() * item.getQuantity();

            // Use the new OrderItem constructor with all fields
            orderItems.add(new OrderItem(
                    item.getProductName(),
                    0, // SKU - use 0 as default or add SKU to CartItem if available
                    item.getQuantity(),
                    item.getPrice(),
                    itemTotal
            ));

            subtotal += itemTotal;
        }

        double taxes = subtotal * TAX_RATE;
        double total = subtotal + DELIVERY_FEE + taxes;

        // Create the new order request structure
        CreateOrderRequest orderRequest = new CreateOrderRequest(
                orderItems,
                subtotal,
                DELIVERY_FEE,
                taxes,
                total
        );

        // Disable button to prevent multiple clicks
        checkoutButton.setEnabled(false);
        checkoutButton.setText("Placing Order...");

        // Make the API call with the new endpoint
        ApiService apiService = RetrofitClient.getApiService(requireContext());
        Call<OrderResponse> call = apiService.createOrder(orderRequest);

        call.enqueue(new Callback<OrderResponse>() {
            @Override
            public void onResponse(Call<OrderResponse> call, Response<OrderResponse> response) {
                if (response.isSuccessful() && response.body() != null) {
                    OrderResponse orderResponse = response.body();

                    if (orderResponse.isSuccess()) {
                        // --- SUCCESS NOTIFICATION ---
                        AppNotification notif = new AppNotification(
                                "Order Placed Successfully",
                                "Order #" + orderResponse.getOrder().getOrderNumber() + " has been received and is being processed.",
                                true
                        );
                        NotificationManager.getInstance().addNotification(notif);

                        // Show success dialog
                        showOrderSuccessDialog(orderResponse.getOrder().getOrderNumber());

                        // Clear the cart
                        CartManager.getInstance().clearCart();
                        if (adapter != null) {
                            adapter.notifyDataSetChanged();
                        }

                        // Update UI
                        updatePriceSummary();

                    } else {
                        // --- API RETURNED SUCCESS=FALSE ---
                        AppNotification notif = new AppNotification(
                                "Order Failed",
                                orderResponse.getMessage(),
                                false
                        );
                        NotificationManager.getInstance().addNotification(notif);

                        Toast.makeText(getContext(),
                                "Order failed: " + orderResponse.getMessage(),
                                Toast.LENGTH_LONG).show();
                    }
                } else {
                    // --- SERVER ERROR NOTIFICATION ---
                    AppNotification notif = new AppNotification(
                            "Order Failed",
                            "We couldn't place your order. The server responded with an error (Code: " + response.code() + "). Please try again later.",
                            false
                    );
                    NotificationManager.getInstance().addNotification(notif);

                    Toast.makeText(getContext(), "Order failed. Please try again.", Toast.LENGTH_LONG).show();
                    Log.e("CartFragment", "API Error: " + response.code());
                }

                // Re-enable the button
                checkoutButton.setEnabled(true);
                checkoutButton.setText("Proceed to Checkout");
            }

            @Override
            public void onFailure(Call<OrderResponse> call, Throwable t) {
                // Network error
                AppNotification notif = new AppNotification(
                        "Order Failed: Network Error",
                        "We couldn't connect to our servers to place your order. Please check your internet connection and try again.",
                        false
                );
                NotificationManager.getInstance().addNotification(notif);

                Toast.makeText(getContext(), "Network error. See notifications for details.", Toast.LENGTH_LONG).show();
                Log.e("CartFragment", "API Network Failure: " + t.getMessage());

                // Re-enable the button
                checkoutButton.setEnabled(true);
                checkoutButton.setText("Proceed to Checkout");
            }
        });
    }

    private void showOrderSuccessDialog(String orderNumber) {
        new AlertDialog.Builder(requireContext())
                .setTitle("Order Placed!")
                .setMessage("Your order #" + orderNumber + " has been successfully placed.")
                .setPositiveButton("View Orders", (dialog, which) -> {
                    // Navigate to orders fragment
                    requireActivity().getSupportFragmentManager().beginTransaction()
                            .replace(R.id.frame_layout, new OrdersFragment())
                            .addToBackStack(null)
                            .commit();
                    dialog.dismiss();
                })
                .setNegativeButton("Continue Shopping", (dialog, which) -> {
                    dialog.dismiss();
                })
                .show();
    }


    private void updatePriceSummary() {
        if (cartItems.isEmpty()) {
            emptyCartMessage.setVisibility(View.VISIBLE);
            recyclerView.setVisibility(View.GONE);
            checkoutButton.setAlpha(0.5f); // Visually indicate disabled state
        } else {
            emptyCartMessage.setVisibility(View.GONE);
            recyclerView.setVisibility(View.VISIBLE);
            checkoutButton.setAlpha(1.0f);
        }

        double subtotal = 0;
        for (CartItem item : cartItems) {
            subtotal += item.getPrice() * item.getQuantity();
        }

        double taxes = subtotal * TAX_RATE;
        double total = subtotal + DELIVERY_FEE + taxes;

        subtotalTextView.setText(String.format(Locale.getDefault(), "R%.2f", subtotal));
        deliveryTextView.setText(String.format(Locale.getDefault(), "R%.2f", DELIVERY_FEE));
        taxesTextView.setText(String.format(Locale.getDefault(), "R%.2f", taxes));
        totalTextView.setText(String.format(Locale.getDefault(), "R%.2f", total));
    }

    // --- Implementation of the adapter's listener methods ---

    @Override
    public void onItemRemoved(int position) {
        cartItems.remove(position);
        adapter.notifyItemRemoved(position);
        adapter.notifyItemRangeChanged(position, cartItems.size()); // Update positions of remaining items
        updatePriceSummary(); // Recalculate totals
    }

    @Override
    public void onQuantityChanged() {
        // Just need to recalculate the totals
        updatePriceSummary();
    }
}
