package com.eduvos.nutec.fragment;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.appcompat.widget.Toolbar;
import androidx.fragment.app.Fragment;
import androidx.recyclerview.widget.LinearLayoutManager;
import androidx.recyclerview.widget.RecyclerView;

import com.eduvos.nutec.adapter.FaqAdapter;
import com.eduvos.nutec.pojo.FaqItem;
import com.eduvos.nutec.R;
import com.google.android.material.bottomnavigation.BottomNavigationView;

import java.util.ArrayList;
import java.util.List;

public class HelpFragment extends Fragment {

    private RecyclerView recyclerView;
    private FaqAdapter adapter;
    private List<FaqItem> faqList;

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        // --- Hide main activity UI elements ---
        toggleMainUI(View.GONE);

        // Inflate the layout for this fragment
        View view = inflater.inflate(R.layout.fragment_help, container, false);

        // --- Setup the fragment's own toolbar ---
        Toolbar helpToolbar;
        helpToolbar = view.findViewById(R.id.help_toolbar);
        helpToolbar.setTitle("Help & FAQ");
        helpToolbar.setTitleTextColor(getResources().getColor(android.R.color.white));

        // Set up the back navigation
        helpToolbar.setNavigationIcon(R.drawable.ic_arrow_back); // You'll need this icon
        helpToolbar.setNavigationOnClickListener(v -> {
            // Use the FragmentManager to pop the back stack, which is like pressing the back button
            requireActivity().getSupportFragmentManager().popBackStack();
        });

        // Setup RecyclerView
        recyclerView = view.findViewById(R.id.faq_recycler_view);
        recyclerView.setLayoutManager(new LinearLayoutManager(getContext()));

        loadFaqData();
        adapter = new FaqAdapter(faqList);
        recyclerView.setAdapter(adapter);

        return view;
    }

    @Override
    public void onDestroyView() {
        super.onDestroyView();
        // --- Restore main activity UI elements when the fragment is destroyed ---
        toggleMainUI(View.VISIBLE);
    }

    /**
     * Helper method to show or hide the main UI elements in MainActivity.
     * @param visibility View.VISIBLE or View.GONE
     */
    private void toggleMainUI(int visibility) {
        // Get a reference to the hosting activity's views
        View header = requireActivity().findViewById(R.id.header_layout);
        BottomNavigationView bottomNav = requireActivity().findViewById(R.id.bottom_navigation);

        if (header != null) {
            header.setVisibility(visibility);
        }
        if (bottomNav != null) {
            bottomNav.setVisibility(visibility);
        }
    }

    private void loadFaqData() {

        faqList = new ArrayList<>();
        faqList.add(new FaqItem("How do I track my order?", "You can track your order status from the 'Orders' section in the menu. Once shipped, a tracking number will be provided."));
        faqList.add(new FaqItem("What are the payment methods available?", "We accept payments via credit card, debit card, and EFT. All transactions are secure and encrypted."));
        faqList.add(new FaqItem("How can I change my delivery address?", "You can update your delivery address in the 'Account' section. Please note that address changes are not possible once an order has been dispatched."));
        faqList.add(new FaqItem("What is your return policy?", "We offer a 30-day return policy for unopened products. Please visit the 'Orders' section to initiate a return request."));
        faqList.add(new FaqItem("How do I contact customer support?", "You can contact our support team via the 'Contact Us' form in the app or by calling our toll-free number during business hours."));
    }
}
