//AccountFragment.java
//        Displays account settings for the user to customise
//        Group Members: Charlene Higgo, Armand Geldenhuys, Aneleh de Lange, Travis Musson, Grant Peterson, Petrus
//        2025
//        Developed for NuTec Inks


package com.eduvos.nutec.fragment;

import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import androidx.fragment.app.Fragment;

import com.eduvos.nutec.R;

public class AccountFragment extends Fragment {
    @Override
    public View onCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState) {
        return inflater.inflate(R.layout.fragment_account, container, false);
    }
}
