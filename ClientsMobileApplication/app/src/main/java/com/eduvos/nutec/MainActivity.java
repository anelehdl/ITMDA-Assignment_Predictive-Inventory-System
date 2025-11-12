package com.eduvos.nutec;

import android.content.Intent;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.view.Menu;
import android.view.MenuItem;
import android.view.View;
import android.widget.TextView;
import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.appcompat.app.AppCompatDelegate;
import androidx.appcompat.widget.PopupMenu;
import androidx.appcompat.widget.Toolbar;
import androidx.fragment.app.Fragment;
import androidx.preference.PreferenceManager;

import com.eduvos.nutec.api.RetrofitClient;
import com.eduvos.nutec.fragment.AccountFragment;
import com.eduvos.nutec.fragment.CartFragment;
import com.eduvos.nutec.fragment.CategoriesFragment;
import com.eduvos.nutec.fragment.HelpFragment;
import com.eduvos.nutec.fragment.HomeFragment;
import com.eduvos.nutec.fragment.ListsFragment;
import com.eduvos.nutec.fragment.NotificationsFragment;
import com.eduvos.nutec.fragment.OrdersFragment;
import com.eduvos.nutec.fragment.ProductsFragment;
import com.eduvos.nutec.fragment.SettingsFragment;
import com.google.android.material.bottomnavigation.BottomNavigationView;

public class MainActivity extends AppCompatActivity {

    //initialise xml components
    private Toolbar toolbar;
    private TextView pageTitle;
    // The SearchView and currentSearchQuery variables have been removed.

    @Override
    protected void onCreate(Bundle savedInstanceState) {

        // --- APPLY THEME AT STARTUP ---
        // Get the preference manager
        SharedPreferences sharedPreferences = PreferenceManager.getDefaultSharedPreferences(this);
        // Check the value of our "dark_mode" preference
        boolean isDarkMode = sharedPreferences.getBoolean("dark_mode", false);
        if (isDarkMode) {
            AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES);
        } else {
            AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO);
        }
        // --- END OF THEME LOGIC ---


        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main);

        // --- 1. Link all XML components first ---
        pageTitle = findViewById(R.id.page_title);
        toolbar = findViewById(R.id.toolbar);
        BottomNavigationView bottomNav = findViewById(R.id.bottom_navigation);
        // The line for findViewById(R.id.searchView) has been removed.

        // --- 2. Configure the Toolbar ---
        setSupportActionBar(toolbar);
        if (getSupportActionBar() != null) {
            getSupportActionBar().setDisplayShowTitleEnabled(false); // Use this to hide default title
        }
//        toolbar.setTitleTextColor(getResources().getColor(android.R.color.white));
//        toolbar.setBackgroundColor(getResources().getColor(R.color.toolbar));
        //left side-burger menu
        toolbar.setNavigationIcon(R.drawable.ic_menu);
        toolbar.setNavigationOnClickListener(v -> showPopupMenu(v));

        // --- 3. The SearchView configuration block has been completely removed. ---

        // --- 4. Configure the Bottom Navigation ---
        bottomNav.setOnItemSelectedListener(item -> {
            Fragment selectedFragment = null;
            int itemId = item.getItemId();
            String title = ""; // Holds new title

            if (itemId == R.id.nav_home) {
                selectedFragment = new HomeFragment();
                title = "Home";
            } else if (itemId == R.id.nav_products) {
                selectedFragment = new ProductsFragment();
                title = "Products";
            } else if (itemId == R.id.nav_categories) {
                selectedFragment = new CategoriesFragment();
                title = "Categories";
            } else if (itemId == R.id.nav_lists) {
                selectedFragment = new ListsFragment();
                title = "Lists";
            } else if (itemId == R.id.nav_account) {
                selectedFragment = new AccountFragment();
                title = "Account";
            }

            if (selectedFragment != null) {
                replaceFragment(selectedFragment, false); // Use a new helper method
                pageTitle.setText(title);
            }
            return true;
        });

        // --- 5. Load the initial fragment LAST ---
        if (savedInstanceState == null) {
            getSupportFragmentManager().beginTransaction().replace(R.id.frame_layout, new HomeFragment()).commit();
            pageTitle.setText("Home");
            bottomNav.setSelectedItemId(R.id.nav_home);
        }

        testApiConnection();
    }

    @Override
    public boolean onCreateOptionsMenu(Menu menu) {
        getMenuInflater().inflate(R.menu.toolbar_menu, menu);
        return true;
    }

    @Override
    public boolean onOptionsItemSelected(@NonNull MenuItem item) {
        int itemId = item.getItemId();
        if (itemId == R.id.menu_cart) {
            replaceFragment(new CartFragment(), true);
            pageTitle.setText("Shopping Cart");
            return true;
        } else if (itemId == R.id.menu_notifications) {
            replaceFragment(new NotificationsFragment(), true);
            pageTitle.setText("Notifications");
            return true;
        }
        return super.onOptionsItemSelected(item);
    }

    private void showPopupMenu(View anchor) {
        PopupMenu popup = new PopupMenu(this, anchor);
        popup.getMenuInflater().inflate(R.menu.popup_menu, popup.getMenu());
        popup.setOnMenuItemClickListener(item -> {
            int itemId = item.getItemId();

            if (itemId == R.id.menu_orders) {
                replaceFragment(new OrdersFragment(), true);
                pageTitle.setText("Orders");
                pageTitle.setTextColor(getResources().getColor(R.color.toolbar_text_white));
                return true;
            } else if (itemId == R.id.menu_settings) {
                replaceFragment(new SettingsFragment(), true);
                pageTitle.setText("Settings");
                pageTitle.setTextColor(getResources().getColor(R.color.toolbar_text_white));
                return true;
            } else if (itemId == R.id.menu_help) {
                replaceFragment(new HelpFragment(), true);
                pageTitle.setText("Help");
                pageTitle.setTextColor(getResources().getColor(R.color.toolbar_text_white));
                return true;
            } else if (itemId == R.id.menu_logout) {
                logoutUser();
                return true;
            }
            return false;
        });
        popup.show();
    }

    private void replaceFragment(Fragment fragment, boolean addToBackStack) {
        var transaction = getSupportFragmentManager().beginTransaction();
        transaction.replace(R.id.frame_layout, fragment);
        if (addToBackStack) {
            transaction.addToBackStack(null);
        }
        transaction.commit();
        pageTitle.setTextColor(getResources().getColor(R.color.toolbar_text_white));
    }

    // The showDetailFragment can be kept or removed, it's a useful helper
    private void showDetailFragment(Fragment fragment) {
        replaceFragment(fragment, true);
    }

    private void logoutUser() {
        // Clear SharedPreferences
        SharedPreferences prefs = getSharedPreferences("MyAppPrefs", MODE_PRIVATE);
        SharedPreferences.Editor editor = prefs.edit();
        editor.clear(); // This removes everything including loggedIn, userName, userEmail, etc.
        editor.apply();

        // Clear the auth token (CRITICAL - this is what LoginActivity checks!)
        RetrofitClient.clearAuthToken(this);

        // Navigate to LoginActivity with flags to clear the back stack
        Intent intent = new Intent(this, LoginActivity.class);
        intent.setFlags(Intent.FLAG_ACTIVITY_NEW_TASK | Intent.FLAG_ACTIVITY_CLEAR_TASK);
        startActivity(intent);
        finish();
    }

    private void testApiConnection() {
        RetrofitClient retrofitClient = new RetrofitClient();
        retrofitClient.pingServer(this);
    }

    // The getCurrentSearchQuery() method has been removed.
}
