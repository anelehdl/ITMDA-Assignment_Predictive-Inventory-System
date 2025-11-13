package com.eduvos.nutec.pojo;

import com.google.gson.annotations.SerializedName;

public class OrderItem {
    @SerializedName("productName")
    private String productName;

    @SerializedName("sku")
    private int sku;

    @SerializedName("quantity")
    private int quantity;

    @SerializedName("pricePerUnit")
    private double pricePerUnit;

    @SerializedName("totalPrice")
    private double totalPrice;

    // Constructor matching your old style (for backward compatibility)
    public OrderItem(String productName, int quantity, double price) {
        this.productName = productName;
        this.quantity = quantity;
        this.pricePerUnit = price;
        this.totalPrice = price * quantity;
        this.sku = 0; // Default SKU
    }

    // New constructor with all fields
    public OrderItem(String productName, int sku, int quantity, double pricePerUnit, double totalPrice) {
        this.productName = productName;
        this.sku = sku;
        this.quantity = quantity;
        this.pricePerUnit = pricePerUnit;
        this.totalPrice = totalPrice;
    }

    // Getters
    public String getProductName() {
        return productName;
    }

    public int getSku() {
        return sku;
    }

    public int getQuantity() {
        return quantity;
    }

    public double getPricePerUnit() {
        return pricePerUnit;
    }

    // Keep this for backward compatibility with your existing code
    public double getPrice() {
        return pricePerUnit;
    }

    public double getTotalPrice() {
        return totalPrice;
    }

    // Setters
    public void setProductName(String productName) {
        this.productName = productName;
    }

    public void setSku(int sku) {
        this.sku = sku;
    }

    public void setQuantity(int quantity) {
        this.quantity = quantity;
        // Recalculate total when quantity changes
        this.totalPrice = this.pricePerUnit * quantity;
    }

    public void setPricePerUnit(double pricePerUnit) {
        this.pricePerUnit = pricePerUnit;
        // Recalculate total when price changes
        this.totalPrice = pricePerUnit * this.quantity;
    }

    public void setPrice(double price) {
        setPricePerUnit(price);
    }

    public void setTotalPrice(double totalPrice) {
        this.totalPrice = totalPrice;
    }
}