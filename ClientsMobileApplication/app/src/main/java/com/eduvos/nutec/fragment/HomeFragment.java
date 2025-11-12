package com.eduvos.nutec.fragment;

import android.annotation.SuppressLint;
import android.content.Context;
import android.content.SharedPreferences;
import android.os.Bundle;
import android.view.LayoutInflater;
import android.view.View;
import android.view.ViewGroup;
import android.widget.TextView;
import android.widget.Toast;

import androidx.annotation.NonNull;
import androidx.annotation.Nullable;
import androidx.fragment.app.Fragment;
import com.eduvos.nutec.R;

public class HomeFragment extends Fragment {

    private TextView welcomeTextView;
    private TextView infoDisplayTextView;

    @Nullable
    @Override
    public View onCreateView(@NonNull LayoutInflater inflater, @Nullable ViewGroup container, @Nullable Bundle savedInstanceState) {
        // Inflate the layout for this fragment
        return inflater.inflate(R.layout.fragment_home, container, false);
    }

    @Override
    public void onViewCreated(@NonNull View view, @Nullable Bundle savedInstanceState) {
        super.onViewCreated(view, savedInstanceState);

        // 1. Find the views within this fragment's layout.
        welcomeTextView = view.findViewById(R.id.welcome_text);
        infoDisplayTextView = view.findViewById(R.id.info_display_text);

        // 2. Set up the welcome message
        if (welcomeTextView != null) {
            SharedPreferences sharedPreferences = requireActivity().getSharedPreferences("MyAppPrefs", Context.MODE_PRIVATE);
            String userName = sharedPreferences.getString("userName", "User");
            welcomeTextView.setText("Welcome, " + userName + "!");
        }

        // 3. Set up all the button listeners
        setupInfoButtonListeners(view);
    }

    /**
     * Finds each button in the layout and attaches a click listener to it.
     * @param fragmentView The root view of the fragment (passed from onViewCreated).
     */
    private void setupInfoButtonListeners(@NonNull View fragmentView) {
        // A helper lambda to reduce repetitive code. It sets the text and shows a small notification.
        // It also checks if infoDisplayTextView is null to prevent crashes.
        @SuppressLint("SetTextI18n") View.OnClickListener listener = v -> {
            if (infoDisplayTextView == null) return; // Safety check

            int id = v.getId();
            if (id == R.id.btn_history) {
                infoDisplayTextView.setText(
                        "Scroll down to view a brief outline of the history of NUtec Digital Ink.\n\n" +

                                "Omnigraphics - 1995\n" +
                                "NUtec Digital Ink founding members Jamie Lowndes and Tony Davison are involved in a management buy-out of Omnigraphics, " +
                                "an early adopter of inkjet digital printing processes and solvent ink development.\n\n" +
                                "Omnigraphics - 1998\n" +
                                "Paul Geldenhuys had now joined the Omnigraphics team and successfully developed the first solvent digital ink " +
                                "for Piezo electric print heads for the VUTEk UltraVu & Idanit 162Ad (Scitex Novo) printers for in-house commercial printing.\n\n" +
                                "TechnoInk - 1999\n" +
                                "A separate company was formed to produce solvent digital ink for Scitex Vision who by then had acquired Idanit.\n\n" +
                                "TechINK - 2001\n" +
                                "Acquired by Scitex Vision, TechnoInk was renamed TechINK, producing all the OEM Scitex Vision solvent and UV curable inks " +
                                "as well as producing alternative inks for the global digital printing ink market. Neil Green joins to head up international " +
                                "sales and marketing and the team that would later form NUtec.\n\n" +
                                "HP-TechINK - 2005\n" +
                                "TechINK was acquired by Hewlett Packard (HP), producing up to 500 tons of solvent and UV cured inks a month, " +
                                "making it one of the largest digital ink manufacturing companies in the world at the time.\n\n" +
                                "NUtec Digital Ink - 2009\n" +
                                "Established by the founders of TechINK, NUtec Digital Ink developed and manufactured digital printing solvent inks " +
                                "for OEMs and distributors for the global market.\n\n" +
                                "NUtec Digital Ink - 2011\n" +
                                "Developed UV curable digital printing inks for Original Equipment Manufacturers (OEMs) and global distributors.\n\n" +
                                "NUtec Digital Ink - 2012\n" +
                                "Developed Environment Responsible Product (ERP) digital inks with no hazardous air pollutants (HAPs) for safer operator environments.\n\n" +
                                "NUtec Digital Ink - 2014\n" +
                                "Developed water-based dye sublimation inks for the global market.\n\n" +
                                "NUtec Digital Ink - 2015\n" +
                                "Developed pigmented water-based inks for industrial printing purposes.\n\n" +
                                "NUtec Digital Ink - 2018\n" +
                                "Expanded water-based ink plant facilities to accommodate growing volumes.\n\n" +
                                "NUtec Digital Ink - 2019\n" +
                                "The company celebrated a decade of growth and innovation.\n\n" +
                                "NUtec Digital Ink - 2021\n" +
                                "NUtec Digital Ink is acquired by the Crosse family and continues to grow its influence and product offering in the international market, " +
                                "with its roots firmly planted in Cape Town, South Africa.\n\n" +
                                "NUtec Digital Ink - 2023\n" +
                                "Demonstrating its commitment to environmental sustainability, the company is awarded OEKO-TEX Eco Passport and GREENGUARD Gold product accreditations.\n\n" +
                                "NUtec Digital Ink - 2024\n" +
                                "Installation of an extensive photovoltaic system generating 400MWh of clean energy annually from the sun, " +
                                "resulting in a combined reduction in carbon emissions of 600 tonnes per year, considerably reducing the plant’s carbon footprint."
                );
                Toast.makeText(getContext(), "Showing History", Toast.LENGTH_SHORT).show();

            } else if (id == R.id.btn_expertise) {
                infoDisplayTextView.setText(
                        "NUtec Digital Ink’s founding members have over a century of combined experience and are recognised as specialists in the ink manufacturing sector.\n\n" +

                                "The company has a widely respected track record in digital ink innovation and creativity with a reputation for rapid development of various ink designs.\n\n" +

                                "As industry pioneers in the design, development & manufacture of digital printing inks, the company offers:\n\n" +

                                "• Long term relationships with OEMs and distribution partners.\n\n" +
                                "• An R&D team focused on providing innovative solutions for demanding applications.\n\n" +
                                "• Development tools which include fully equipped laboratories and print rooms.\n\n" +
                                "• Multiple quality control stages for every product manufactured.\n\n" +
                                "• Full traceability and accountability for each ink batch produced."
                );
                Toast.makeText(getContext(), "Showing Expertise", Toast.LENGTH_SHORT).show();

            } else if (id == R.id.btn_manufacturing) {
                infoDisplayTextView.setText(
                        "NUtec manufactures OEM quality digital printing ink with batch-to-batch consistency, long-run print reliability and exceptional image quality.\n\n" +

                                "Inks are produced through proprietary, state of the art manufacturing processes and offered in a wide variety of packaging solutions including pouches/bags, cartridges and bottles.\n\n" +

                                "NUtec’s unique 4-stage quality control system ensures the highest quality of products from raw material to finished ink.\n\n" +

                                "Pigment dispersions are created at the very beginning of the manufacturing process, ensuring particle size consistency and milling at the nano level.\n\n" +

                                "Proprietary equipment and procedures allow complete control over the entire manufacturing process, ensuring only the highest quality ink before final packaging.\n\n" +

                                "The NUtec packaging and labelling department packages inks to customer’s specifications, while the regulatory department ensures that all international chemical safety regulations are followed.\n\n" +

                                "An in-house maintenance and engineering department services and customizes all NUtec plant and machinery, ensuring smooth running and limited downtime in manufacturing.\n\n" +

                                "Once the product leaves the factory, a dedicated logistics and customer care team handles and monitors the shipping and distribution of NUtec inks."
                );
                Toast.makeText(getContext(), "Showing Manufacturing", Toast.LENGTH_SHORT).show();

            } else if (id == R.id.btn_rnd) {
                infoDisplayTextView.setText(
                        "NUtec employs a dynamic Research and Development (R&D) team which includes:\n\n" +

                                "• Chemists specialising in UV curable, water-based and solvent digital ink development.\n\n" +
                                "• Print head, rheology and waveform specialists.\n\n" +
                                "• Integration specialists for single pass and multi pass systems.\n\n" +
                                "• Engineering specialists for auxiliary component design.\n\n" +

                                "Development tools based at NUtec’s premises include:\n\n" +

                                "• Print head laboratories with drop watcher and waveform analysis tools.\n\n" +
                                "• Independent UV curable, water-based and solvent digital ink laboratories.\n\n" +
                                "• R&D print rooms with the relevant printer platforms for ink development.\n\n" +
                                "• Environmental simulation rooms to replicate seasonal temperature and humidity conditions.\n\n" +
                                "• Complete ink characterisation to ISO standards to ensure final product and application performance.\n\n" +
                                "• State of the art accelerated aging test facility.\n\n" +

                                "The NUtec R&D team has developed, amongst others, an Environment Responsible Product (ERP) line of digital printing inks that contain no Hazardous Air Pollutants (HAPs) as defined by the Environmental Protection Agency (EPA), making the inks safer for both the printer operator and for the environment."
                );
                Toast.makeText(getContext(), "Showing R&D", Toast.LENGTH_SHORT).show();

            } else if (id == R.id.btn_labs) {
                infoDisplayTextView.setText(
                        "At NUtec, analytical chemists verify the robustness of all ink designs throughout the multiple stages of Quality Control (QC).\n\n" +

                                "Quality control tests conducted by the QC team include:\n\n" +

                                "• Surface tension\n\n" +
                                "• Viscosity\n\n" +
                                "• Particle size\n\n" +
                                "• Printing properties\n\n" +
                                "• Wear & tear\n\n" +
                                "• Crack stress testing\n\n" +
                                "• Colour\n\n" +

                                "Shelf life stability tests are also performed to ensure product longevity and reliability.\n\n" +

                                "Retained samples of final products are held in store for traceability.\n\n" +

                                "The NUtec quality system ensures reliable batch-to-batch consistency."
                );
                Toast.makeText(getContext(), "Showing Labs & QC", Toast.LENGTH_SHORT).show();

            } else if (id == R.id.btn_compliance) {
                infoDisplayTextView.setText(
                        "NUtec employs a dedicated team to ensure that all inks produced comply with international regulations for both manufacture and shipping.\n\n" +

                                "As an example, this includes assisting clients in Europe to be REACH compliant.\n\n" +

                                "Country and region specific safety labelling laws, as well as chemical handling laws, are referenced.\n\n" +

                                "Safety Data Sheets (SDS) are produced to ensure that all NUtec products adhere to local and international regulations before leaving the factories.\n\n" +

                                "This includes compliance with the latest GHS (Globally Harmonised System) chemical classification and labelling requirements.\n\n" +

                                "The NUtec Chemical Regulatory and Compliance Department continuously monitors international laws to ensure product labelling and classifications remain up to date.\n\n" +

                                "For any chemical compliance queries, kindly contact: regulatory@nutecdigital.com"
                );
                Toast.makeText(getContext(), "Showing Compliance", Toast.LENGTH_SHORT).show();

            } else if (id == R.id.btn_products) {
                infoDisplayTextView.setText(
                        "Product Range\n\n" +

                                "UV-cured Inks\n" +
                                "NUtec’s Ruby, Amethyst and Quartz ranges of UV-curable digital inks are designed for either roll-to-roll, hybrid or rigid printer applications with either conventional or LED lamp curing systems.\n\n" +

                                "Water-based Inks\n" +
                                "The Aquamarine range of water-based digital inks are designed for dye sublimation transfer applications onto polyester-based substrates including textiles, garments, flags and banners.\n\n" +

                                "Eco-solvent Inks\n" +
                                "NUtec’s range of eco-solvent digital inks offers low smell, superior abrasion and chemical resistance, while providing excellent media compatibility across a broad range of self-adhesive and flexible substrates.\n\n" +

                                "Environment Responsible Inks\n" +
                                "The Emerald ink range consists of Environment Responsible Product (ERP) inks offering a greener solution for the environment, as they contain no hazardous air pollutants and are safer for the operator environment.\n\n" +

                                "Mild Solvent Inks\n" +
                                "NUtec’s range of mild solvent inks is cost-effective and robust, offering good reliability with excellent media compatibility across a broad range of self-adhesive and flexible substrates.\n\n" +

                                "Cleaning Solutions\n" +
                                "NUtec manufactures a range of cleaning and flushing solutions for our water-based, UV-curable and solvent digital inks. Note that no other cleaning solution is to be used with NUtec inks.\n\n" +

                                "Coatings, Varnishes & Liquid Laminates\n" +
                                "NUcoat is a gloss UV stable single-component water-based clear coat system and is compatible with most UV cured and solvent-based inks, with a very wide media compatibility.\n\n" +

                                "Bulk Ink Delivery Systems\n" +
                                "NUtec has developed a bulk ink solution to deliver a continuous supply of ink, allowing more efficient printing with uninterrupted refilling.\n\n" +

                                "Kindly note: Not all products are available in every country or region. For region-specific inks, please contact: sales@nutecdigital.com"
                );
                Toast.makeText(getContext(), "Showing Product Range", Toast.LENGTH_SHORT).show();

            } else if (id == R.id.btn_support) {
                infoDisplayTextView.setText(
                        "Tech Support\n\n" +

                                "Colour Management\n" +
                                "NUtec Digital Ink offers the following colour management value-added services:\n\n" +
                                "International support: \n\n" +
                                "• RIP specific profiling\n\n" +
                                "• Colour management\n\n" +
                                "• Colour workflow implementation\n\n" +
                                "• Workshops & training\n\n" +

                                "Printer Conversion\n" +
                                "NUtec offers the following conversion services and training when converting a printer to NUtec digital inks:\n\n" +
                                "International support: \n\n" +
                                "• Colour profiling\n\n" +
                                "• Training and development\n\n" +
                                "• Guided on-site conversion\n\n" +
                                "Product documentation:\n" +
                                "• Instruction manuals\n" +
                                "• Trouble-shooting guides\n" +
                                "• Maintenance guides\n\n" +
                                "Additional resources:\n" +
                                "• Technical papers\n" +
                                "• Testimonials\n\n" +

                                "Training & Development\n" +
                                "NUtec Digital Ink offers the following training and development services:\n\n" +
                                "International support:\n" +
                                "• Product training\n" +
                                "• Sales & marketing support\n\n" +
                                "Product documentation:\n" +
                                "• Brochures and technical datasheets\n" +
                                "• Safety data sheets\n" +
                                "• Tutorials & manuals\n" +
                                "• Trouble-shooting guides\n" +
                                "• Maintenance guides\n\n" +
                                "Additional resources:\n" +
                                "• Technical papers\n" +
                                "• Online demos\n" +
                                "• Instructional videos"
                );
                Toast.makeText(getContext(), "Showing Tech Support", Toast.LENGTH_SHORT).show();

            } else if (id == R.id.btn_contact) {
                infoDisplayTextView.setText("Connect with us through our various channels. We are here to answer your questions and explore potential collaborations.");
                Toast.makeText(getContext(), "Showing Contact Info", Toast.LENGTH_SHORT).show();
            }
        };

        // Find each button and assign the same listener to all of them.
        fragmentView.findViewById(R.id.btn_history).setOnClickListener(listener);
        fragmentView.findViewById(R.id.btn_expertise).setOnClickListener(listener);
        fragmentView.findViewById(R.id.btn_manufacturing).setOnClickListener(listener);
        fragmentView.findViewById(R.id.btn_rnd).setOnClickListener(listener);
        fragmentView.findViewById(R.id.btn_labs).setOnClickListener(listener);
        fragmentView.findViewById(R.id.btn_compliance).setOnClickListener(listener);
        fragmentView.findViewById(R.id.btn_products).setOnClickListener(listener);
        fragmentView.findViewById(R.id.btn_support).setOnClickListener(listener);
        fragmentView.findViewById(R.id.btn_contact).setOnClickListener(listener);
    }
}
