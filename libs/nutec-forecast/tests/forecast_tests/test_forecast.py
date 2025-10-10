from nutec_forecast import AsyncForcaster
from nutec_forecast.models import default_model_loader
import unittest
from unittest.mock import MagicMock, patch


class MockModel:
    def __init__(self):
        pass

    def predict(self, var):
        pass


class AysncForecasterTest(unittest.TestCase):
    async def test_create_models(self):
        forecaster = AsyncForcaster(MockModel, default_model_loader, model_count=2)
        actual = forecaster.model_queue.qsize()
        expected = 2
        self.assertEqual(actual, expected)

        model = await forecaster.model_queue.get()
        self.assertIsInstance(model, MockModel)

    @patch.object(MockModel, "predict", return_value={"test": 42.0})
    async def test_predict(self, mock_predict):
        forecaster = AsyncForcaster(MockModel, default_model_loader, model_count=1)
        mock_params = MagicMock()
        result = await forecaster.predict(mock_params)
        self.assertEqual(result, {"test": 42.0})

        with self.assertRaises(RuntimeError) as cm:
            forecaster.model_queue.clear()
            await forecaster.predict(mock_params)
        self.assertIn("No model available in queue", str(cm.exception))
