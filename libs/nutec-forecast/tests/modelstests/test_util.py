from nutec_forecast.util.time_series_util import (
    get_client_item_time_series_features,
    __time_series_features,
)
import unittest
import pandas as pd


class UtilTests(unittest.TestCase):
    def setUp(self):
        self.df = pd.DataFrame(
            {
                "cust_id": ["A", "A", "B"],
                "item": ["X", "X", "Y"],
                "qty_lag1": [1, 2, 3],
                "qty_lag5": [0.5, 0.6, 0.7],
                "qty_lag10": [0.1, 0.2, 0.3],
                "rolling_mean_3": [1.1, 1.2, 1.3],
                "rolling_std_3": [0.1, 0.2, 0.3],
                "rolling_mean_5": [1.5, 1.6, 1.7],
                "rolling_std_5": [0.2, 0.3, 0.4],
                "rolling_mean_10": [1.0, 1.1, 1.2],
                "rolling_std_10": [0.1, 0.2, 0.3],
                "days_since_client_purchase": [1, 2, 3],
                "days_since_client_item_purchase": [0, 1, 2],
            }
        )

    def test_normal_case(self):
        result = get_client_item_time_series_features(self.df, "A", "X")
        self.assertEqual(result["qty_lag1"], 2)  # last row for A/X

    def test_client_item_not_found(self):
        # NO CLIENT AND ITEM
        with self.assertRaises(ValueError) as cm:
            get_client_item_time_series_features(self.df, "9", "Z")
        self.assertIn("No data for client=9 and item=Z", str(cm.exception))

        # NO ITEM
        with self.assertRaises(ValueError) as cm:
            get_client_item_time_series_features(self.df, "A", "1")
        self.assertIn("No data for client=A and item=1", str(cm.exception))

    def test_missing_id_column(self):
        df_missing_col = self.df.drop(columns=["cust_id"])
        with self.assertRaises(ValueError) as cm:
            get_client_item_time_series_features(df_missing_col, "A", "X")
        self.assertIn("Missing required features in dataframe", str(cm.exception))

    def test_missing_time_series_feature(self):
        df_missing_ts = self.df.drop(columns=["rolling_mean_3"])
        with self.assertRaises(ValueError) as cm:
            get_client_item_time_series_features(df_missing_ts, "A", "X")
        self.assertIn("Missing required features in dataframe", str(cm.exception))
