package com.eduvos.nutec.pojo;

import com.google.gson.annotations.SerializedName;

public class ProductOrder {

    @SerializedName("sku")
    private int sku;

    @SerializedName("skuDescription")
    private String skuDescription;

    @SerializedName("litres")
    private int litres;

    @SerializedName("userCode")
    private String userCode;

    @SerializedName("orderDate")
    private String orderDate;

    @SerializedName("previousOrderDate")
    private String previousOrderDate;

    @SerializedName("daysBetweenOrders")
    private int daysBetweenOrders;

    @SerializedName("averageDailyUse")
    private Double averageDailyUse;

    @SerializedName("userId")
    private String userId;

    // GETTERS
    public int getSku() {
        return sku;
    }

    public String getSkuDescription() {
        return skuDescription;
    }

    public int getLitres() {
        return litres;
    }

    public String getUserCode() {
        return userCode;
    }

    public String getOrderDate() {
        return orderDate;
    }

    public int getDaysBetweenOrders() {
        return daysBetweenOrders;
    }

    public Double getAverageDailyUse() {
        return averageDailyUse;
    }

    // SETTERS
    public void setSku(int sku) {
        this.sku = sku;
    }

    public void setSkuDescription(String skuDescription) {
        this.skuDescription = skuDescription;
    }

    public void setLitres(int litres) {
        this.litres = litres;
    }
}
