package com.eduvos.nutec.adapter;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.R;
import com.eduvos.nutec.pojo.OrderItem;

import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

public class OrderDetailItemsAdapter extends RecyclerView.Adapter<OrderDetailItemsAdapter.ItemViewHolder> {

    private List<OrderItem> items = new ArrayList<>();

    public void setItems(List<OrderItem> items) {
        this.items = items != null ? items : new ArrayList<>();
        notifyDataSetChanged();
    }

    @NonNull
    @Override
    public ItemViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_order_detail, parent, false);
        return new ItemViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull ItemViewHolder holder, int position) {
        OrderItem item = items.get(position);

        holder.productName.setText(item.getProductName());
        holder.productQuantity.setText("Qty: " + item.getQuantity());
        holder.productPrice.setText(String.format(Locale.getDefault(), "R%.2f each", item.getPricePerUnit()));
        holder.productTotal.setText(String.format(Locale.getDefault(), "R%.2f", item.getTotalPrice()));
    }

    @Override
    public int getItemCount() {
        return items.size();
    }

    static class ItemViewHolder extends RecyclerView.ViewHolder {
        TextView productName;
        TextView productQuantity;
        TextView productPrice;
        TextView productTotal;

        public ItemViewHolder(@NonNull View itemView) {
            super(itemView);
            productName = itemView.findViewById(R.id.product_name);
            productQuantity = itemView.findViewById(R.id.product_quantity);
            productPrice = itemView.findViewById(R.id.product_price);
            productTotal = itemView.findViewById(R.id.product_total);
        }
    }
}