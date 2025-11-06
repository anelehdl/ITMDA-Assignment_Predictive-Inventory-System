package com.eduvos.nutec.pojo;

import com.google.gson.annotations.SerializedName;
import java.util.List;

// Represents the entire order object that will be sent to the API
public class OrderPayload {

    // Use @SerializedName if your JSON keys are different from field names
    @SerializedName("items")
    private List<OrderItem> items;

    @SerializedName("subtotal")
    private double subtotal;

    @SerializedName("delivery_fee")
    private double deliveryFee;

    @SerializedName("taxes")
    private double taxes;

    @SerializedName("total_amount")
    private double totalAmount;



    public OrderPayload(List<OrderItem> items, double subtotal, double deliveryFee, double taxes, double totalAmount) {
        this.items = items;
        this.subtotal = subtotal;
        this.deliveryFee = deliveryFee;
        this.taxes = taxes;
        this.totalAmount = totalAmount;
    }
}


