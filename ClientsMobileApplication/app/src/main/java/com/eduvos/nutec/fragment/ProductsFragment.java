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
import androidx.appcompat.app.AlertDialog;
import androidx.appcompat.widget.SearchView;
import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.pojo.ProductItem;
import com.google.android.material.button.MaterialButton;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Comparator;
import java.util.List;
import java.util.stream.Collectors;

import retrofit2.Call;
import retrofit2.Callback;
import retrofit2.Response;

import com.eduvos.nutec.adapter.ProductAdapter;
import com.eduvos.nutec.R;
import com.eduvos.nutec.pojo.ProductOrder;
import com.eduvos.nutec.api.ApiService;
import com.eduvos.nutec.api.RetrofitClient;
import com.eduvos.nutec.manager.CartManager;
import com.eduvos.nutec.manager.WishlistManager;
import com.google.android.material.snackbar.Snackbar;

public class ProductsFragment extends Fragment implements ProductAdapter.OnProductActionClickListener{

    private RecyclerView recyclerView;
    private ProductAdapter adapter;
    private ProgressBar progressBar;
    private TextView emptyMessageView;
    private MaterialButton sortButton;
    private SearchView searchView;

    // Two lists are key: one for the original data, one for display
    private final List<ProductOrder> fullProductList = new ArrayList<>();
    private final List<ProductOrder> displayedProductList = new ArrayList<>();

    private int currentSortMethod = 0; // 0: Default

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        View view = inflater.inflate(R.layout.fragment_products, container, false);

        initializeViews(view);
        setupRecyclerView();
        setupSearch();
        setupSort();

        return view;
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);
        // Only fetch if the list is empty to avoid re-fetching on screen rotation
        if (fullProductList.isEmpty()) {
            fetchProducts();
        }
    }

    private void initializeViews(View view) {
        recyclerView = view.findViewById(R.id.products_recycler_view);
        progressBar = view.findViewById(R.id.progress_bar);
        emptyMessageView = view.findViewById(R.id.empty_list_message);
        sortButton = view.findViewById(R.id.sort_button);
        searchView = view.findViewById(R.id.products_search_view);
    }

    private void setupRecyclerView() {
        recyclerView.setLayoutManager(new LinearLayoutManager(getContext()));
        // The adapter always points to the list that is being displayed
        adapter = new ProductAdapter(displayedProductList, this);
        recyclerView.setAdapter(adapter);
    }

    private void setupSearch() {
        searchView.setOnQueryTextListener(new SearchView.OnQueryTextListener() {
            @Override
            public boolean onQueryTextSubmit(String query) {
                return false; // We filter on text change
            }

            @Override
            public boolean onQueryTextChange(String newText) {
                filterAndSortList(); // Re-run the filter and sort logic
                return true;
            }
        });
    }

    private void setupSort() {
        sortButton.setOnClickListener(v -> showSortDialog());
    }

    private void showSortDialog() {
        final String[] sortOptions = {
                "Default",
                "Name (A-Z)",
                "Name (Z-A)",
                "Litres (Low to High)",
                "Litres (High to Low)"
        };

        new AlertDialog.Builder(requireContext())
                .setTitle("Sort By")
                .setSingleChoiceItems(sortOptions, currentSortMethod, (dialog, which) -> {
                    currentSortMethod = which;
                    filterAndSortList(); // Re-run the filter and sort logic
                    dialog.dismiss();
                })
                .show();
    }

    /**
     * Central method to handle all filtering and sorting.
     * It always starts from the full list, applies the search query, then applies the sort.
     */
    private void filterAndSortList() {
        // Step 1: Filter the list based on the search query
        String query = searchView.getQuery().toString();
        List<ProductOrder> filteredList;

        if (query.isEmpty()) {
            filteredList = new ArrayList<>(fullProductList);
        } else {
            String lowerCaseQuery = query.toLowerCase();
            filteredList = fullProductList.stream()
                    .filter(item -> item.getSkuDescription().toLowerCase().contains(lowerCaseQuery))
                    .collect(Collectors.toList());
        }

        // Step 2: Sort the already filtered list
        switch (currentSortMethod) {
            case 1: // Name ASC
                Collections.sort(filteredList, Comparator.comparing(ProductOrder::getSkuDescription, String.CASE_INSENSITIVE_ORDER));
                break;
            case 2: // Name DESC
                Collections.sort(filteredList, Comparator.comparing(ProductOrder::getSkuDescription, String.CASE_INSENSITIVE_ORDER).reversed());
                break;
            case 3: // Litres ASC
                Collections.sort(filteredList, Comparator.comparingInt(ProductOrder::getLitres));
                break;
            case 4: // Litres DESC
                Collections.sort(filteredList, Comparator.comparingInt(ProductOrder::getLitres).reversed());
                break;
            // Case 0 (Default) requires no sorting after filtering
        }

        // Step 3: Update the display list and notify the adapter
        displayedProductList.clear();
        displayedProductList.addAll(filteredList);
        adapter.notifyDataSetChanged();
        updateEmptyView();
    }

    private void fetchProducts() {
        if (getView() == null) return;
        progressBar.setVisibility(View.VISIBLE);
        recyclerView.setVisibility(View.GONE);
        emptyMessageView.setVisibility(View.GONE);

        ApiService apiService = RetrofitClient.getApiService(requireContext());
        Call<List<ProductOrder>> call = apiService.getProductOrders();

        call.enqueue(new Callback<List<ProductOrder>>() {
            @Override
            public void onResponse(@NonNull Call<List<ProductOrder>> call, @NonNull Response<List<ProductOrder>> response) {
                if (!isAdded() || getView() == null) return;
                progressBar.setVisibility(View.GONE);

                if (response.isSuccessful() && response.body() != null) {
                    fullProductList.clear();
                    fullProductList.addAll(response.body());
                    filterAndSortList(); // Initial population of the displayed list
                } else {
                    Toast.makeText(getContext(), "Failed to retrieve data.", Toast.LENGTH_SHORT).show();
                    updateEmptyView();
                }
            }

            @Override
            public void onFailure(@NonNull Call<List<ProductOrder>> call, @NonNull Throwable t) {
                if (!isAdded()) return;
                progressBar.setVisibility(View.GONE);
                Log.e("ProductsFragment", "Network request failed", t);
                Toast.makeText(getContext(), "Network Error.", Toast.LENGTH_SHORT).show();
                updateEmptyView();
            }
        });
    }

    private void updateEmptyView() {
        if (getView() == null) return;

        if (displayedProductList.isEmpty()) {
            recyclerView.setVisibility(View.GONE);
            emptyMessageView.setVisibility(View.VISIBLE);
            // Give a more helpful message
            emptyMessageView.setText(fullProductList.isEmpty() ? "No products available." : "No results found.");
        } else {
            recyclerView.setVisibility(View.VISIBLE);
            emptyMessageView.setVisibility(View.GONE);
        }
    }

    @Override
    public void onAddToWishlistClick(ProductOrder product) {
        ProductItem productItem = new ProductItem(
                product.getSkuDescription(),
                45
        );
        // Use the WishlistManager singleton to add or remove the item
        boolean added = WishlistManager.getInstance().toggleWishlist(productItem);
        // Create the confirmation message based on the action
        String message = added ? product.getSkuDescription() + " added to wishlist" : product.getSkuDescription() + " removed from wishlist";

        Snackbar snackbar = Snackbar.make(requireView(), message, Snackbar.LENGTH_SHORT);

        // Only show the "View Wishlist" action if the item was added
        if (added) {
            snackbar.setAction("View Wishlist", v -> {
                // Navigate to the ListsFragment (or your wishlist screen)
                requireActivity().getSupportFragmentManager().beginTransaction()
                        .replace(R.id.frame_layout, new ListsFragment())
                        .addToBackStack(null) // Allows user to return
                        .commit();
            });
        }

        snackbar.show();
    }

    @Override
    public void onAddToCartClick(ProductOrder product) {
        ProductItem productItem = new ProductItem(
                product.getSkuDescription(),
                45
        );

        // Use the CartManager singleton to add the item
        CartManager.getInstance().addToCart(productItem);

        // Show a Snackbar message with an action to view the cart
        Snackbar.make(requireView(), product.getSkuDescription() + " added to cart", Snackbar.LENGTH_SHORT)
                .setAction("View Cart", v -> {
                    // Navigate to the CartFragment
                    requireActivity().getSupportFragmentManager().beginTransaction()
                            .replace(R.id.frame_layout, new CartFragment())
                            .addToBackStack(null) // Allows user to return
                            .commit();
                })
                .show();
    }
}
