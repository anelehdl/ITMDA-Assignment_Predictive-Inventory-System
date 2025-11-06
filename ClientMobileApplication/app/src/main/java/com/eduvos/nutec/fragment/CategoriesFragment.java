package com.eduvos.nutec.fragment;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;

import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.manager.CartManager;
import com.eduvos.nutec.adapter.CategoriesAdapter;
import com.eduvos.nutec.pojo.CategoryHeader;
import com.eduvos.nutec.pojo.ListItem;
import com.eduvos.nutec.pojo.ProductItem;
import com.eduvos.nutec.R;
import com.eduvos.nutec.manager.WishlistManager;
import com.google.android.material.snackbar.Snackbar;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;


public class CategoriesFragment extends Fragment implements CategoriesAdapter.OnProductActionClickListener {

    private RecyclerView recyclerView;
    private CategoriesAdapter adapter;

    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        View view = inflater.inflate(R.layout.fragment_categories, container, false);

        recyclerView = view.findViewById(R.id.categories_recycler_view);
        recyclerView.setLayoutManager(new LinearLayoutManager(getContext()));

        // Load and set up the data
        List<ListItem> categorizedList = buildCategorizedList(getSampleProducts());
        // Pass 'this' as the listener when creating the adapter
        adapter = new CategoriesAdapter(categorizedList, this);
        recyclerView.setAdapter(adapter);

        return view;
    }

    // ---  INTERFACE METHODS ---

    @Override
    public void onAddToCartClick(ProductItem product) {
        // --- Use the CartManager to add the item ---
        CartManager.getInstance().addToCart(product);

        // Show a more informative Snackbar message
        Snackbar.make(requireView(), product.getName() + " added to cart", Snackbar.LENGTH_SHORT)
                .setAction("View Cart", v -> {
                    // Navigate to the CartFragment
                    requireActivity().getSupportFragmentManager().beginTransaction()
                            .replace(R.id.frame_layout, new CartFragment())
                            .addToBackStack(null)
                            .commit();
                })
                .show();
    }

    @Override
    public void onAddToWishlistClick(ProductItem product) {
        // Use the WishlistManager to add or remove the item
        boolean added = WishlistManager.getInstance().toggleWishlist(product);

        // Create the confirmation message based on whether the item was added or removed
        String message = added ? product.getName() + " added to wishlist" : product.getName() + " removed from wishlist";

        // --- USE A SNACKBAR INSTEAD OF A TOAST ---
        Snackbar snackbar = Snackbar.make(requireView(), message, Snackbar.LENGTH_SHORT);

        // Only show the "View Wishlist" action if the item was added
        if (added) {
            snackbar.setAction("View Wishlist", v -> {
                // Navigate to the ListsFragment (your wishlist screen)
                requireActivity().getSupportFragmentManager().beginTransaction()
                        .replace(R.id.frame_layout, new ListsFragment())
                        .addToBackStack(null) // Allows the user to press back to return to categories
                        .commit();
            });
        }

        snackbar.show();
    }



    private List<ListItem> buildCategorizedList(List<ProductItem> products) {
        List<ListItem> items = new ArrayList<>();
        List<ProductItem> under50 = new ArrayList<>();
        List<ProductItem> under100 = new ArrayList<>();
        List<ProductItem> over100 = new ArrayList<>();

        // Sort products into price buckets
        for (ProductItem product : products) {
            if (product.getPrice() < 50) {
                under50.add(product);
            } else if (product.getPrice() < 100) {
                under100.add(product);
            } else {
                over100.add(product);
            }
        }

        // Build the final list with headers and products
        if (!under50.isEmpty()) {
            items.add(new CategoryHeader("Under R50"));
            items.addAll(under50);
        }

        if (!under100.isEmpty()) {
            items.add(new CategoryHeader("Under R100"));
            items.addAll(under100);
        }

        if (!over100.isEmpty()) {
            items.add(new CategoryHeader("R100 and Over"));
            items.addAll(over100);
        }

        return items;
    }

    private List<ProductItem> getSampleProducts() {
        // In a real app, you would fetch this from your database/API
        List<ProductItem> products = new ArrayList<>();
        products.add(new ProductItem("Ink Solvent 250ml", 35.50));
        products.add(new ProductItem("Cleaning Wipes (10-pack)", 49.99));
        products.add(new ProductItem("Ink 1245 Cyan 2000ml", 85.50));
        products.add(new ProductItem("Standard Magenta Ink 1L", 95.00));
        products.add(new ProductItem("Ink 2652 Magenta 5L", 120.00));
        products.add(new ProductItem("High-Capacity Black Ink", 250.00));
        products.add(new ProductItem("Industrial Cleaner 5L", 180.75));
        products.add(new ProductItem("Specialty Yellow Ink 1L", 89.00));
        products.add(new ProductItem("Maintenance Kit", 450.00));

        // Sort products by price to ensure they are grouped nicely
        Collections.sort(products, (p1, p2) -> Double.compare(p1.getPrice(), p2.getPrice()));

        return products;
    }
}
