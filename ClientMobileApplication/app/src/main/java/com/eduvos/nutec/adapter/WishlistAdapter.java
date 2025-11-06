package com.eduvos.nutec.adapter;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageButton;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.pojo.ProductItem;
import com.eduvos.nutec.R;

import java.util.List;
import java.util.Locale;

public class WishlistAdapter extends RecyclerView.Adapter<WishlistAdapter.WishlistViewHolder> {

    private List<ProductItem> wishlistItems;
    private OnWishlistActionClickListener listener;

    // Interface to handle clicks on the "remove" or "add to cart" buttons
    public interface OnWishlistActionClickListener {
        void onRemoveFromWishlist(ProductItem product, int position);
        void onMoveToCart(ProductItem product, int position);
    }

    public WishlistAdapter(List<ProductItem> wishlistItems, OnWishlistActionClickListener listener) {
        this.wishlistItems = wishlistItems;
        this.listener = listener;
    }

    @NonNull
    @Override
    public WishlistViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        // We can reuse the same layout from the categories page
        View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_category_product, parent, false);
        return new WishlistViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull WishlistViewHolder holder, int position) {
        ProductItem product = wishlistItems.get(position);

        holder.productName.setText(product.getName());
        holder.productPrice.setText(String.format(Locale.getDefault(), "R%.2f", product.getPrice()));

        // Since this is the wishlist, the icon should always be a solid heart
        holder.wishlistButton.setImageResource(R.drawable.ic_favorite_filled);

        // Set click listeners for the buttons
        holder.wishlistButton.setOnClickListener(v -> {
            if (listener != null) {
                listener.onRemoveFromWishlist(product, holder.getAdapterPosition());
            }
        });

        holder.addToCartButton.setOnClickListener(v -> {
            if (listener != null) {
                listener.onMoveToCart(product, holder.getAdapterPosition());
            }
        });
    }

    @Override
    public int getItemCount() {
        return wishlistItems.size();
    }

    // ViewHolder for a single wishlist item
    static class WishlistViewHolder extends RecyclerView.ViewHolder {
        TextView productName, productPrice;
        ImageButton addToCartButton, wishlistButton;

        WishlistViewHolder(@NonNull View itemView) {
            super(itemView);
            productName = itemView.findViewById(R.id.product_category_name);
            productPrice = itemView.findViewById(R.id.product_category_price);
            addToCartButton = itemView.findViewById(R.id.button_add_to_cart);
            wishlistButton = itemView.findViewById(R.id.button_add_to_wishlist);
        }
    }
}
