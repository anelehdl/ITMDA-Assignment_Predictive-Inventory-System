package com.eduvos.nutec.manager;

import com.eduvos.nutec.pojo.ProductItem;

import java.util.ArrayList;
import java.util.List;

public class WishlistManager {

    private static WishlistManager instance;
    private List<ProductItem> wishlistItems = new ArrayList<>();

    // Private constructor to ensure it's a singleton
    private WishlistManager() {}

    // Public method to get the single instance of this class
    public static synchronized WishlistManager getInstance() {
        if (instance == null) {
            instance = new WishlistManager();
        }
        return instance;
    }

    /**
     * Toggles a product's state in the wishlist.
     * If it's not in the list, it gets added.
     * If it's already in the list, it gets removed.
     * @param product The product to add or remove.
     * @return true if the item was added, false if it was removed.
     */
    public boolean toggleWishlist(ProductItem product) {
        for (ProductItem item : wishlistItems) {
            // Use a unique identifier if available, otherwise name is okay for this example
            if (item.getName().equals(product.getName())) {
                wishlistItems.remove(item); // Item was in the list, so remove it
                return false;
            }
        }
        wishlistItems.add(product); // Item was not in the list, so add it
        return true;
    }

    /**
     * Checks if a product is in the wishlist.
     * @param product The product to check.
     * @return true if the item is in the wishlist, false otherwise.
     */
    public boolean isProductInWishlist(ProductItem product) {
        for (ProductItem item : wishlistItems) {
            if (item.getName().equals(product.getName())) {
                return true;
            }
        }
        return false;
    }

    public List<ProductItem> getWishlistItems() {
        return wishlistItems;
    }
}
