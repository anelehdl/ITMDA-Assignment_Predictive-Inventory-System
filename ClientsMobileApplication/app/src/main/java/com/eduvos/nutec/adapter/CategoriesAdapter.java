package com.eduvos.nutec.adapter;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageButton; // Import ImageButton
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.pojo.CategoryHeader;
import com.eduvos.nutec.pojo.ListItem;
import com.eduvos.nutec.pojo.ProductItem;
import com.eduvos.nutec.R;
import com.eduvos.nutec.manager.WishlistManager;

import java.util.List;
import java.util.Locale;

public class CategoriesAdapter extends RecyclerView.Adapter<RecyclerView.ViewHolder> {

    private List<ListItem> items;
    private OnProductActionClickListener actionListener; // Listener for button clicks

    // --- INTERFACE FOR CLICK EVENTS ---
    public interface OnProductActionClickListener {
        void onAddToCartClick(ProductItem product);
        void onAddToWishlistClick(ProductItem product);
    }

    public CategoriesAdapter(List<ListItem> items, OnProductActionClickListener listener) {
        this.items = items;
        this.actionListener = listener; // Set the listener
    }

    @Override
    public int getItemViewType(int position) {
        return items.get(position).getItemType();
    }

    @NonNull
    @Override
    public RecyclerView.ViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        LayoutInflater inflater = LayoutInflater.from(parent.getContext());
        if (viewType == ListItem.TYPE_HEADER) {
            View view = inflater.inflate(R.layout.item_category_header, parent, false);
            return new HeaderViewHolder(view);
        } else { // TYPE_PRODUCT
            View view = inflater.inflate(R.layout.item_category_product, parent, false);
            return new ProductViewHolder(view);
        }
    }

    @Override
    public void onBindViewHolder(@NonNull RecyclerView.ViewHolder holder, int position) {
        int viewType = getItemViewType(position);
        if (viewType == ListItem.TYPE_HEADER) {
            CategoryHeader header = (CategoryHeader) items.get(position);
            HeaderViewHolder headerViewHolder = (HeaderViewHolder) holder;
            headerViewHolder.headerTitle.setText(header.getTitle());
        } else { // TYPE_PRODUCT
            ProductItem product = (ProductItem) items.get(position);
            ProductViewHolder productViewHolder = (ProductViewHolder) holder;
            productViewHolder.productName.setText(product.getName());
            productViewHolder.productPrice.setText(String.format(Locale.getDefault(), "R%.2f", product.getPrice()));
            // --- CHECK WISHLIST STATUS AND SET THE ICON ---
            if (WishlistManager.getInstance().isProductInWishlist(product)) {
                productViewHolder.addToWishlistButton.setImageResource(R.drawable.ic_favorite_filled);
            } else {
                productViewHolder.addToWishlistButton.setImageResource(R.drawable.ic_favorite_border);
            }
            // --- SET CLICK LISTENERS FOR THE BUTTONS ---
            productViewHolder.addToCartButton.setOnClickListener(v -> {
                if (actionListener != null) {
                    actionListener.onAddToCartClick(product);
                }
            });

            productViewHolder.addToWishlistButton.setOnClickListener(v -> {
                if (actionListener != null) {
                    actionListener.onAddToWishlistClick(product);
                    // Notify the adapter that this specific item has changed,
                    // so it can redraw the icon immediately.
                    notifyItemChanged(holder.getAdapterPosition());
                }
            });
        }
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    // ViewHolder for Headers
    static class HeaderViewHolder extends RecyclerView.ViewHolder {
        TextView headerTitle;
        HeaderViewHolder(@NonNull View itemView) {
            super(itemView);
            headerTitle = itemView.findViewById(R.id.category_header_title);
        }
    }

    // ViewHolder for Products - UPDATED
    static class ProductViewHolder extends RecyclerView.ViewHolder {
        TextView productName, productPrice;
        ImageButton addToCartButton, addToWishlistButton; // Add buttons

        ProductViewHolder(@NonNull View itemView) {
            super(itemView);
            productName = itemView.findViewById(R.id.product_category_name);
            productPrice = itemView.findViewById(R.id.product_category_price);
            // Link the buttons from the layout
            addToCartButton = itemView.findViewById(R.id.button_add_to_cart);
            addToWishlistButton = itemView.findViewById(R.id.button_add_to_wishlist);
        }
    }
}
