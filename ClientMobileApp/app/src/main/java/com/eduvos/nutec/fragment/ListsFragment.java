package com.eduvos.nutec.fragment;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;
import android.widget.Toast;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.manager.CartManager;
import com.eduvos.nutec.pojo.ProductItem;
import com.eduvos.nutec.R;
import com.eduvos.nutec.adapter.WishlistAdapter;
import com.eduvos.nutec.manager.WishlistManager;
import com.google.android.material.snackbar.Snackbar;

import java.util.List;

// Implement the adapter's click listener interface
public class ListsFragment extends Fragment implements WishlistAdapter.OnWishlistActionClickListener {

    private RecyclerView recyclerView;
    private WishlistAdapter adapter;
    private List<ProductItem> wishlistItems;
    private TextView emptyWishlistMessage;

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        // Use a generic fragment layout that contains a RecyclerView and an empty message view.

        View view = inflater.inflate(R.layout.fragment_list_view, container, false);

        recyclerView = view.findViewById(R.id.list_recycler_view);
        emptyWishlistMessage = view.findViewById(R.id.empty_list_message);

        // Set the message for this specific list
        emptyWishlistMessage.setText("Your wishlist is empty.");

        return view;
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        // Get the data from our singleton manager
        wishlistItems = WishlistManager.getInstance().getWishlistItems();

        // Set up the RecyclerView
        recyclerView.setLayoutManager(new LinearLayoutManager(getContext()));
        adapter = new WishlistAdapter(wishlistItems, this);
        recyclerView.setAdapter(adapter);

        updateEmptyView();
    }

    private void updateEmptyView() {
        if (wishlistItems.isEmpty()) {
            recyclerView.setVisibility(View.GONE);
            emptyWishlistMessage.setVisibility(View.VISIBLE);
        } else {
            recyclerView.setVisibility(View.VISIBLE);
            emptyWishlistMessage.setVisibility(View.GONE);
        }
    }

    @Override
    public void onResume() {
        super.onResume();
        // Refresh the list every time the user comes back to this screen,
        // in case they made changes elsewhere.
        if (adapter != null) {
            adapter.notifyDataSetChanged();
            updateEmptyView();
        }
    }

    // --- Handle Clicks from the Adapter ---

    @Override
    public void onRemoveFromWishlist(ProductItem product, int position) {
        // Remove from the manager
        WishlistManager.getInstance().toggleWishlist(product);
        // Notify the adapter to remove the item from the view
        adapter.notifyItemRemoved(position);
        adapter.notifyItemRangeChanged(position, wishlistItems.size());
        updateEmptyView();
        Toast.makeText(getContext(), product.getName() + " removed from wishlist", Toast.LENGTH_SHORT).show();
    }

    @Override
    public void onMoveToCart(ProductItem product, int position) {
        // Add item to cart
        CartManager.getInstance().addToCart(product);
        // Remove item from wishlist
        WishlistManager.getInstance().toggleWishlist(product);

        // Notify the adapter to remove the item from the view
        adapter.notifyItemRemoved(position);
        adapter.notifyItemRangeChanged(position, wishlistItems.size());
        updateEmptyView();

        // Show a confirmation message with a "View Cart" action
        Snackbar.make(requireView(), product.getName() + " moved to cart", Snackbar.LENGTH_SHORT)
                .setAction("View Cart", v ->
                        requireActivity().getSupportFragmentManager().beginTransaction()
                                .replace(R.id.frame_layout, new CartFragment())
                                .addToBackStack(null)
                                .commit())
                .show();
    }
}
