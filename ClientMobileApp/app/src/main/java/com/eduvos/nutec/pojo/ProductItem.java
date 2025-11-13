package com.eduvos.nutec.pojo;
//used in wishlist and cart

public class ProductItem implements ListItem {
    private String name;
    private double price;


    public ProductItem(String name, double price) {
        this.name = name;
        this.price = price;
    }

    public String getName() {
        return name;
    }

    public double getPrice() {
        return price;
    }

    @Override
    public int getItemType() {
        return TYPE_PRODUCT;
    }
}

