package com.eduvos.nutec.pojo;

import com.google.gson.annotations.SerializedName;
import java.util.List;

public class OrdersListResponse {
    @SerializedName("success")
    private boolean success;

    @SerializedName("orders")
    private List<Order> orders;

    public boolean isSuccess() { return success; }
    public List<Order> getOrders() { return orders; }
}