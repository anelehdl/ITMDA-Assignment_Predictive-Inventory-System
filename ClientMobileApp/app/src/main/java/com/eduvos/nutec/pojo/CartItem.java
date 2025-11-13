package com.eduvos.nutec.pojo;

public class CartItem {
    private String productName;
    private double price;
    private int quantity;


    public CartItem(String productName, double price, int quantity) {
        this.productName = productName;
        this.price = price;
        this.quantity = quantity;
    }

    // --- Getters ---
    public String getProductName() {
        return productName;
    }

    public double getPrice() {
        return price;
    }

    public int getQuantity() {
        return quantity;
    }

    // --- Setters ---
    public void setQuantity(int quantity) {
        this.quantity = quantity;
    }
}
