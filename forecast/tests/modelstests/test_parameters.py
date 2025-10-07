from forecast.models import ParameterAdaptor, ClientItemAdaptor

import pandas as pd
import numpy as np
import unittest
from unittest.mock import MagicMock, patch


class MockParameter(ParameterAdaptor):
    def __init__(self, feats):
        super().__init__(feats)

    def transform(self):
        pass


class ParameterAdaptorTest(unittest.TestCase):
    def test_init(self):
        features = ["A", "B", "C"]
        params = MockParameter(features)
        self.assertIs(params.features, features)
        self.assertEqual(len(params.parameters()), 0)

    def test_empty_features(self):
        features = []
        with self.assertRaises(ValueError) as cm:
            params = MockParameter(features)

    def test_validate_rawdata(self):
        features = ["A", "B", "C"]
        params = MockParameter(features)
        param_values = pd.DataFrame({"A": [1, 2, 3], "B": [1, 2, 3], "C": [1, 2, 3]})
        params.to_dataframe(param_values)

    def test_invalidate_rawdata(self):
        features = ["A", "B", "C"]
        params = MockParameter(features)
        param_values = pd.DataFrame({"A": [1, 2, 3], "C": [1, 2, 3]})

        with self.assertRaises(ValueError) as cm:
            params.to_dataframe(param_values)

    def test_to_dataframe(self):
        features = ["A", "B"]
        params = MockParameter(features)
        params_dict = {"A": [1, 2, 3], "B": [1, 2, 3]}
        param_values = pd.DataFrame({"A": [1, 2, 3], "B": [1, 2, 3]})

        df = params.to_dataframe(params_dict)

        self.assertIsInstance(df, pd.DataFrame)
        self.assertListEqual(list(df.columns), features)
        self.assertListEqual(df["A"].to_list(), [1, 2, 3])
        self.assertListEqual(df["B"].to_list(), [1, 2, 3])

    def test_empty_to_dataframe(self):
        features = ["A", "B"]
        params = MockParameter(features)
        params_dict = {"A": [], "B": []}
        df = params.to_dataframe(params_dict)

        self.assertIsInstance(df, pd.DataFrame)
        self.assertEqual(df.shape, (0, 2))


class ClientItemParameterTest(unittest.TestCase):
    def setUp(self):
        self.data = pd.DataFrame(
            {
                "item": ["A", "B"],
                "date": pd.to_datetime(["2025-10-01", "2025-10-02"]),
                "cust_name": ["Alice", "Bob"],
                "cust_code": ["C001", "C002"],
                "cust_id": [1, 2],
                "price": [100, 200],
                "region": ["North", "South"],
                "area": ["X", "Y"],
                "color": ["Red", "Blue"],
                "container": ["Box", "Bag"],
                "currency": ["ZAR", "ZAR"],
            }
        )

        # Patch ParameterAdaptor.to_dataframe to return our test data
        patcher = patch.object(
            ClientItemAdaptor, "to_dataframe", return_value=self.data
        )
        self.addCleanup(patcher.stop)
        self.mock_to_dataframe = patcher.start()

        self.adaptor = ClientItemAdaptor()

    def test_transform_columns(self):
        self.adaptor.transform(self.data)
        df = self.adaptor.input

        # Check categorical conversions
        for col in [
            "item",
            "region",
            "area",
            "cust_id",
            "cust_code",
            "color",
            "container",
            "currency",
            "category",
        ]:
            self.assertTrue(pd.api.types.is_categorical_dtype(df[col]))

        # Check category column
        self.assertTrue(
            all(
                df["category"]
                == (df["color"].astype(str) + "_" + df["container"].astype(str))
            )
        )

        # Check price log transform
        np.testing.assert_array_almost_equal(df["price"].values, np.log1p([100, 200]))

        # Check time-based features
        time_cols = [
            "dayofweek",
            "dayofweek_sin",
            "dayofweek_cos",
            "month",
            "month_sin",
            "month_cos",
            "dayofyear",
            "dayofyear_sin",
            "dayofyear_cos",
            "day",
            "day_sin",
            "day_cos",
            "year",
            "quarter",
            "quarter_sin",
            "quarter_cos",
        ]
        for col in time_cols:
            self.assertIn(col, df.columns)

        # Check input stored in self.input
        self.assertIs(df, self.adaptor.input)
