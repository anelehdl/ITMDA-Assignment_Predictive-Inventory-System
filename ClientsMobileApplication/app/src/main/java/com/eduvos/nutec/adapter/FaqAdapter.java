package com.eduvos.nutec.adapter;

import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.ImageView;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.pojo.FaqItem;
import com.eduvos.nutec.R;

import java.util.List;

public class FaqAdapter extends RecyclerView.Adapter<FaqAdapter.FaqViewHolder> {

    private List<FaqItem> faqList;

    public FaqAdapter(List<FaqItem> faqList) {
        this.faqList = faqList;
    }

    @NonNull
    @Override
    public FaqViewHolder onCreateViewHolder(@NonNull ViewGroup parent, int viewType) {
        View view = LayoutInflater.from(parent.getContext()).inflate(R.layout.item_faq, parent, false);
        return new FaqViewHolder(view);
    }

    @Override
    public void onBindViewHolder(@NonNull FaqViewHolder holder, int position) {
        FaqItem currentItem = faqList.get(position);
        holder.questionTextView.setText(currentItem.getQuestion());
        holder.answerTextView.setText(currentItem.getAnswer());

        // Set the visibility based on the expanded state
        boolean isExpanded = currentItem.isExpanded();
        holder.answerTextView.setVisibility(isExpanded ? View.VISIBLE : View.GONE);
        holder.arrowIcon.setRotation(isExpanded ? 180f : 0f); // Rotate arrow

        // Set an OnClickListener on the question layout
        holder.itemView.setOnClickListener(v -> {
            // Toggle the expanded state
            currentItem.setExpanded(!isExpanded);
            // Notify the adapter that this item has changed to trigger a re-bind
            notifyItemChanged(position);
        });
    }

    @Override
    public int getItemCount() {
        return faqList.size();
    }

    // ViewHolder class
    public static class FaqViewHolder extends RecyclerView.ViewHolder {
        TextView questionTextView;
        TextView answerTextView;
        ImageView arrowIcon;

        public FaqViewHolder(@NonNull View itemView) {
            super(itemView);
            questionTextView = itemView.findViewById(R.id.faq_question);
            answerTextView = itemView.findViewById(R.id.faq_answer);
            arrowIcon = itemView.findViewById(R.id.arrow_icon);
        }
    }
}
