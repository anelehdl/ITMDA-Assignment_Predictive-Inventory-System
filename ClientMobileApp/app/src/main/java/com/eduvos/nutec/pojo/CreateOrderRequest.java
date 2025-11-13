package com.eduvos.nutec.pojo;

import com.google.gson.annotations.SerializedName;
import java.util.List;

public class CreateOrderRequest {
    @SerializedName("items")
    private List<OrderItem> items;

    @SerializedName("subtotal")
    private double subtotal;

    @SerializedName("deliveryFee")
    private double deliveryFee;

    @SerializedName("taxAmount")
    private double taxAmount;

    @SerializedName("total")
    private double total;

    public CreateOrderRequest(List<OrderItem> items, double subtotal, double deliveryFee,
                              double taxAmount, double total) {
        this.items = items;
        this.subtotal = subtotal;
        this.deliveryFee = deliveryFee;
        this.taxAmount = taxAmount;
        this.total = total;
    }

    // Getters
    public List<OrderItem> getItems() { return items; }
    public double getSubtotal() { return subtotal; }
    public double getDeliveryFee() { return deliveryFee; }
    public double getTaxAmount() { return taxAmount; }
    public double getTotal() { return total; }
}