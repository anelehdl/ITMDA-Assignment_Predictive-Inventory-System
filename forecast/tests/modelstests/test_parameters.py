from forecast.models import ParameterAdaptor, ClientItemAdaptor

import pandas as pd
import unittest
from unittest.mock import MagicMock


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
    pass
