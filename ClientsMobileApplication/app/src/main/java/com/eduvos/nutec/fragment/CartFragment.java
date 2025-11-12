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

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

import com.eduvos.nutec.adapter.CartAdapter;
import com.eduvos.nutec.R;
import com.eduvos.nutec.pojo.CartItem;
import com.eduvos.nutec.pojo.OrderItem;
import com.eduvos.nutec.pojo.OrderPayload;
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
        // Build the OrderPayload from the cart data
        List<OrderItem> orderItems = new ArrayList<>();
        for (CartItem item : cartItems) {
            orderItems.add(new OrderItem(item.getProductName(), item.getQuantity(), item.getPrice()));
        }

        double subtotal = 0;
        for (CartItem item : cartItems) {
            subtotal += item.getPrice() * item.getQuantity();
        }
        double taxes = subtotal * TAX_RATE;
        double total = subtotal + DELIVERY_FEE + taxes;

        OrderPayload orderPayload = new OrderPayload(orderItems, subtotal, DELIVERY_FEE, taxes, total);

        //  Disable button to prevent multiple clicks
        checkoutButton.setEnabled(false);
        checkoutButton.setText("Placing Order...");

        //  Make the API call
        ApiService apiService = RetrofitClient.getApiService(requireContext());
        Call<Void> call = apiService.submitOrder(orderPayload);

        call.enqueue(new Callback<Void>() {
            @Override
            public void onResponse(Call<Void> call, Response<Void> response) {
                if (response.isSuccessful()) {
                    // --- SUCCESS NOTIFICATION ---
                    AppNotification notif = new AppNotification(
                            "Order Placed Successfully",
                            "Your order has been received and is being processed. You will be notified once it ships.",
                            true
                    );

                    NotificationManager.getInstance().addNotification(notif);


                    // Order was successful
                    showOrderSuccessDialog();
                    // Use the CartManager to clear the global cart list
                    CartManager.getInstance().clearCart();
                    if (adapter != null) {
                        // Notify the adapter that the data has been removed so the UI can update
                        adapter.notifyDataSetChanged();
                    }

                    adapter.notifyDataSetChanged();

                    // Recalculate the price summary, which will now show R0.00 and the "empty" message
                    updatePriceSummary();


                } else {
                    // --- SERVER ERROR NOTIFICATION ---
                    AppNotification notif = new AppNotification(
                            "Order Failed",
                            "We couldn't place your order. The server responded with an error (Code: " + response.code() + "). Please try again later.",
                            false
                    );
                    NotificationManager.getInstance().addNotification(notif);

                    // API returned an error (e.g., 404, 500)
                    Toast.makeText(getContext(), "Order failed. Please try again.", Toast.LENGTH_LONG).show();
                    Log.e("CartFragment", "API Error: " + response.code());
                }
                // Re-enable the button
                checkoutButton.setEnabled(true);
                checkoutButton.setText("Proceed to Checkout");
            }

            @Override
            public void onFailure(Call<Void> call, Throwable t) {
                // Network error (e.g., no internet connection)
                // --- NETWORK FAILURE NOTIFICATION ---
                AppNotification notif = new AppNotification(
                        "Order Failed: Network Error",
                        "We couldn't connect to our servers to place your order. Please check your internet connection and try again.",
                        false // false for failure
                );
                NotificationManager.getInstance().addNotification(notif);

                // Show a simple toast to let the user know to check notifications
                Toast.makeText(getContext(), "Network error. See notifications for details.", Toast.LENGTH_LONG).show();
                Log.e("CartFragment", "API Network Failure: " + t.getMessage());

                // Re-enable the button
                checkoutButton.setEnabled(true);
                checkoutButton.setText("Proceed to Checkout");
            }
        });
    }

    private void showOrderSuccessDialog() {
        new AlertDialog.Builder(requireContext())
                .setTitle("Order Placed!")
                .setMessage("Your order has been successfully sent.")
                .setPositiveButton("OK", (dialog, which) -> {
                    // Optionally navigate back to the home screen
                    // requireActivity().getSupportFragmentManager().beginTransaction()
                    //        .replace(R.id.frame_layout, new HomeFragment())
                    //        .commit();
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
