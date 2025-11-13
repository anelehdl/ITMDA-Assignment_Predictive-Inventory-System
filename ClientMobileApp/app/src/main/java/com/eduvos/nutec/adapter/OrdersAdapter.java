package com.eduvos.nutec.adapter;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;

import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.R;
import com.eduvos.nutec.pojo.Order;

import java.text.ParseException;
import java.text.SimpleDateFormat;
import java.util.Date;
import java.util.List;
import java.util.Locale;

public class OrdersAdapter extends RecyclerView.Adapter<OrdersAdapter.OrderViewHolder> {

    private List<Order> ordersList;
    private OnOrderClickListener listener;

    public interface OnOrderClickListener {
        void onOrderClick(Order order);
    }

    public OrdersAdapter(List<Order> ordersList, OnOrderClickListener listener) {
        this.ordersList = ordersList;
        this.listener = listener;
    }

    @NonNull
    @Override
    public OrderViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext())
                .inflate(R.layout.item_order, parent, false);
        return new OrderViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull OrderViewHolder holder, int position) {
        Order order = ordersList.get(position);

        holder.orderNumber.setText("Order #" + order.getOrderNumber());
        holder.orderStatus.setText(order.getStatus());
        holder.orderTotal.setText(String.format(Locale.getDefault(), "R%.2f", order.getTotal()));
        holder.orderDate.setText(formatDate(order.getCreatedAt()));
        holder.itemCount.setText(order.getItems().size() + " items");

        // Set status color
        if ("Pending".equals(order.getStatus())) {
            holder.orderStatus.setTextColor(holder.itemView.getContext().getColor(android.R.color.holo_orange_dark));
        } else if ("Completed".equals(order.getStatus())) {
            holder.orderStatus.setTextColor(holder.itemView.getContext().getColor(android.R.color.holo_green_dark));
        }

        holder.itemView.setOnClickListener(v -> {
            if (listener != null) {
                listener.onOrderClick(order);
            }
        });
    }

    @Override
    public int getItemCount() {
        return ordersList.size();
    }

    private String formatDate(String dateString) {
        try {
            SimpleDateFormat inputFormat = new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss", Locale.getDefault());
            SimpleDateFormat outputFormat = new SimpleDateFormat("MMM dd, yyyy", Locale.getDefault());
            Date date = inputFormat.parse(dateString);
            return outputFormat.format(date);
        } catch (ParseException e) {
            return dateString;
        }
    }

    static class OrderViewHolder extends RecyclerView.ViewHolder {
        TextView orderNumber;
        TextView orderStatus;
        TextView orderTotal;
        TextView orderDate;
        TextView itemCount;

        public OrderViewHolder(@NonNull View itemView) {
            super(itemView);
            orderNumber = itemView.findViewById(R.id.order_number);
            orderStatus = itemView.findViewById(R.id.order_status);
            orderTotal = itemView.findViewById(R.id.order_total);
            orderDate = itemView.findViewById(R.id.order_date);
            itemCount = itemView.findViewById(R.id.item_count);
        }
    }
}