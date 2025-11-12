package com.eduvos.nutec.pojo;

public class CategoryHeader implements ListItem {
    private String title;

    public CategoryHeader(String title) {

        this.title = title;
    }

    public String getTitle() {

        return title;
    }

    @Override
    public int getItemType() {

        return TYPE_HEADER;
    }
}

