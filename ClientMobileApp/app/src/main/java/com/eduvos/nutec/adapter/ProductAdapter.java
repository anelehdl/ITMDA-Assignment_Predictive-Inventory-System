package com.eduvos.nutec.adapter;

import android.util.Log;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.Button;
import android.widget.ImageButton;
import android.widget.ImageView;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;
import java.util.List;
import com.eduvos.nutec.pojo.ProductOrder;
import com.eduvos.nutec.R;

public class ProductAdapter extends RecyclerView.Adapter<ProductAdapter.ProductViewHolder> {

    private List<ProductOrder> productList;
    private OnProductActionClickListener listener; // Listener for button clicks

    // Define an interface for click events
    public interface OnProductActionClickListener {
        void onAddToCartClick(ProductOrder product);
        void onAddToWishlistClick(ProductOrder product);
    }

    public ProductAdapter(List<ProductOrder> productList, OnProductActionClickListener listener) {
        this.productList = productList;
        this.listener = listener;
    }//:D

    @NonNull
    @Override
    public ProductViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_product, parent, false);
        return new ProductViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull ProductViewHolder holder, int position) {
        ProductOrder product = productList.get(position);

        String description = product.getSkuDescription();
        int sku = product.getSku();

        // Add logging to debug
        Log.d("ProductAdapter", "Binding product: " + description + " (SKU: " + sku + ")");

        holder.productName.setText(description);
        holder.productSku.setText("SKU: " + sku);

        // Set click listeners for the buttons
        holder.addToCartButton.setOnClickListener(v -> {
            if (listener != null) {
                listener.onAddToCartClick(product);
            }
        });

        holder.addToWishlistButton.setOnClickListener(v -> {
            if (listener != null) {
                listener.onAddToWishlistClick(product);
            }
        });
    }

    @Override
    public int getItemCount() {
        return productList.size();
    }

    // ViewHolder class
    public static class ProductViewHolder extends RecyclerView.ViewHolder {
        TextView productName;
        TextView productSku;
        ImageView addToCartButton;
        ImageView addToWishlistButton;

        public ProductViewHolder(@NonNull View itemView) {
            super(itemView);
            productName = itemView.findViewById(R.id.product_name);
            productSku = itemView.findViewById(R.id.product_sku);
            addToCartButton = itemView.findViewById(R.id.add_to_cart_button);
            addToWishlistButton = itemView.findViewById(R.id.add_to_wishlist_button);
        }
    }
}