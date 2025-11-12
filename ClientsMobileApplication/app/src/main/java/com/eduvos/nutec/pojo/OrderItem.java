package com.eduvos.nutec.pojo;

// Represents a single item within the order payload
public class OrderItem {
    // You can customize these fields to match what your API expects
    private String productName;
    private int quantity;
    private double price;

    public OrderItem(String productName, int quantity, double price) {
        this.productName = productName;
        this.quantity = quantity;
        this.price = price;
    }

    // Getters are needed for Gson to serialize the object
    public String getProductName() {

        return productName;
    }

    public int getQuantity() {

        return quantity;
    }

    public double getPrice() {

        return price;
    }
}


