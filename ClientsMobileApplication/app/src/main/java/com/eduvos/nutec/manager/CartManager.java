package com.eduvos.nutec.manager;

import com.eduvos.nutec.pojo.CartItem;
import com.eduvos.nutec.pojo.ProductItem;

import java.util.ArrayList;
import java.util.List;

public class CartManager {

    private static CartManager instance;
    private List<CartItem> cartItems = new ArrayList<>();

    // Private constructor to prevent anyone else from creating an instance
    private CartManager() {}

    // The only way to get the instance of this class
    public static synchronized CartManager getInstance() {
        if (instance == null) {
            instance = new CartManager();
        }
        return instance;
    }

    public void addToCart(ProductItem product) {
        // Check if the item is already in the cart
        for (CartItem item : cartItems) {
            if (item.getProductName().equals(product.getName())) {
                // If it is, just increase the quantity
                item.setQuantity(item.getQuantity() + 1);
                return; // Exit the method
            }
        }

        // If the item is not in the cart, add it as a new item
        cartItems.add(new CartItem(product.getName(), product.getPrice(), 1));
    }

    public List<CartItem> getCartItems() {
        return cartItems;
    }

    public void clearCart() {
        cartItems.clear();
    }
}
