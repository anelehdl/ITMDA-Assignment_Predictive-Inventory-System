from nutec_forecast.models import DirectQuantileForecaster
import unittest

from unittest.mock import MagicMock, Mock, patch
import numpy as np


class TestModel:
    pass

def mock_loader(path):
    with open(path, 'r') as f:
        pass

class DirectQuantileForecasterTest(unittest.TestCase):
    def test_creates_model_names(self):
        forecaster = DirectQuantileForecaster("lgb", quantiles=[10, 50, 90], horizon=1)

        expected = {f"q{i}": None for i in [10, 50, 90]}
        self.assertEqual(expected, forecaster.models)

    def test_load_models(self):
        mock_load = Mock()
        forecaster = DirectQuantileForecaster(
            "horizon_forcast", quantiles=[10], horizon=1
        )
        path = "testpath"
        model = TestModel()
        mock_load.return_value = model
        self.assertIsNone(forecaster.models["q10"])
        forecaster.load(path, mock_load)
        self.assertEqual(model, forecaster.models["q10"])
    
    @patch("builtins.open")
    def test_load_models_no_path(self, mockfile):
        forecaster = DirectQuantileForecaster(
            "horizon_forcast", quantiles=[10], horizon=1
        )
        path = "testpath"
        mockfile.side_effect = FileNotFoundError

        with self.assertRaises(FileNotFoundError):
            forecaster.load(path, mock_loader)
        

    def test_predict(self):
        mock_params = MagicMock()
        mock_model = MagicMock()
        mock_params.parameters.return_value = ["test"]
        forecaster = DirectQuantileForecaster(
            "horizon_forcast", quantiles=[10], horizon=1
        )

        output = np.log1p(10)
        expected = np.expm1(output)
        mock_model.predict.return_value = np.array([output])
        forecaster.models["q10"] = mock_model

        prediction = forecaster.predict(mock_params)
      
        self.assertEqual(prediction["q10"], expected)
