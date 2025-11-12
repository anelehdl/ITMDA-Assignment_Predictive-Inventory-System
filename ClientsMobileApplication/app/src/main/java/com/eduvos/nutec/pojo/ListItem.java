package com.eduvos.nutec.pojo;

// A marker interface to allow different object types in the same list
public interface ListItem {
    int TYPE_HEADER = 0;
    int TYPE_PRODUCT = 1;

    int getItemType(

    );
}

