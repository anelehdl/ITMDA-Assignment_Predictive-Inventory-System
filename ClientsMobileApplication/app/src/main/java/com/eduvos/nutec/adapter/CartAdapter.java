//CartAdapter.java
//
//        Group Members: Charlene Higgo, Armand Geldenhuys, Aneleh de Lange, Travis Musson, Grant Peterson, Petrus
//        2025
//        Developed for NuTec Inks


package com.eduvos.nutec.adapter;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageButton;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.pojo.CartItem;
import com.eduvos.nutec.R;

import java.util.List;
import java.util.Locale;

public class CartAdapter extends RecyclerView.Adapter<CartAdapter.CartViewHolder> {

    private List<CartItem> cartItems;
    private OnCartItemChangedListener listener;

    // Interface to communicate back to the fragment
    public interface OnCartItemChangedListener {
        void onItemRemoved(int position);
        void onQuantityChanged();
    }

    public CartAdapter(List<CartItem> cartItems, OnCartItemChangedListener listener) {
        this.cartItems = cartItems;
        this.listener = listener;
    }

    @NonNull
    @Override
    public CartViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_cart_product, parent, false);
        return new CartViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull CartViewHolder holder, int position) {
        CartItem currentItem = cartItems.get(position);

        holder.productName.setText(currentItem.getProductName());
        holder.productPrice.setText(String.format(Locale.getDefault(), "R%.2f", currentItem.getPrice()));
        holder.productQuantity.setText(String.valueOf(currentItem.getQuantity()));

        // --- Click Listeners for buttons ---
        holder.increaseQuantity.setOnClickListener(v -> {
            int currentQuantity = currentItem.getQuantity();
            currentItem.setQuantity(currentQuantity + 1);
            notifyItemChanged(position); // Update this item
            if (listener != null) {
                listener.onQuantityChanged(); // Notify fragment to update totals
            }
        });

        holder.decreaseQuantity.setOnClickListener(v -> {
            int currentQuantity = currentItem.getQuantity();
            if (currentQuantity > 1) {
                currentItem.setQuantity(currentQuantity - 1);
                notifyItemChanged(position); // Update this item
            } else {
                // If quantity is 1, decreasing removes the item
                if (listener != null) {
                    listener.onItemRemoved(position);
                }
            }
            if (listener != null) {
                listener.onQuantityChanged(); // Notify fragment to update totals
            }
        });

        holder.removeItem.setOnClickListener(v -> {
            if (listener != null) {
                listener.onItemRemoved(position); // Notify fragment to remove item
            }
        });
    }

    @Override
    public int getItemCount() {
        return cartItems != null ? cartItems.size() : 0;
    }


    // --- ViewHolder Class ---
    public static class CartViewHolder extends RecyclerView.ViewHolder {
        TextView productName, productPrice, productQuantity;
        ImageButton increaseQuantity, decreaseQuantity, removeItem;

        public CartViewHolder(@NonNull View itemView) {
            super(itemView);
            productName = itemView.findViewById(R.id.cart_product_name);
            productPrice = itemView.findViewById(R.id.cart_product_price);
            productQuantity = itemView.findViewById(R.id.cart_product_quantity);
            increaseQuantity = itemView.findViewById(R.id.button_increase_quantity);
            decreaseQuantity = itemView.findViewById(R.id.button_decrease_quantity);
            removeItem = itemView.findViewById(R.id.button_remove_from_cart);
        }
    }
}

