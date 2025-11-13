package com.eduvos.nutec.pojo;

import com.google.gson.annotations.SerializedName;
import java.util.ArrayList;
import java.util.Date;
import java.util.List;

public class Order {
    @SerializedName("id")
    private String id;

    @SerializedName("orderNumber")
    private String orderNumber;

    @SerializedName("items")
    private List<OrderItem> items = new ArrayList<>();

    @SerializedName("subtotal")
    private double subtotal;

    @SerializedName("deliveryFee")
    private double deliveryFee;

    @SerializedName("taxAmount")
    private double taxAmount;

    @SerializedName("total")
    private double total;

    @SerializedName("status")
    private String status;

    @SerializedName("createdAt")
    private String createdAt;

    // Getters
    public String getId() { return id; }
    public String getOrderNumber() { return orderNumber; }
    public List<OrderItem> getItems() { return items; }
    public double getSubtotal() { return subtotal; }
    public double getDeliveryFee() { return deliveryFee; }
    public double getTaxAmount() { return taxAmount; }
    public double getTotal() { return total; }
    public String getStatus() { return status; }
    public String getCreatedAt() { return createdAt; }

    // Setters
    public void setId(String id) { this.id = id; }
    public void setOrderNumber(String orderNumber) { this.orderNumber = orderNumber; }
    public void setItems(List<OrderItem> items) { this.items = items; }
    public void setSubtotal(double subtotal) { this.subtotal = subtotal; }
    public void setDeliveryFee(double deliveryFee) { this.deliveryFee = deliveryFee; }
    public void setTaxAmount(double taxAmount) { this.taxAmount = taxAmount; }
    public void setTotal(double total) { this.total = total; }
    public void setStatus(String status) { this.status = status; }
    public void setCreatedAt(String createdAt) { this.createdAt = createdAt; }
}