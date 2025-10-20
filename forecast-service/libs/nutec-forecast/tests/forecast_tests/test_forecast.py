from nutec_forecast import AsyncForecaster
from nutec_forecast.models import default_model_loader
import unittest
from unittest.mock import MagicMock, patch


class MockModel:
    def __init__(self):
        pass

    def predict(self, var):
        pass


class AysncForecasterTest(unittest.IsolatedAsyncioTestCase):
    async def test_create_models(self):
        forecaster = AsyncForecaster(MagicMock, default_model_loader, model_count=2)
        actual = forecaster.model_queue.qsize()
        expected = 2
        self.assertEqual(actual, expected)

        model = await forecaster.model_queue.get()
        self.assertIsInstance(model, MagicMock)

    async def test_load_created_models(self):
        mock_loader = MagicMock()
        mock_loader.return_value = "model"
        forecaster = AsyncForecaster(MagicMock, mock_loader, model_count=2)
        # forecaster.model_queue = models
        path = "test_path"
        forecaster.load(path)

        # for model in models:
        # model.load.assert_called_once_with(path, mock_loader)

    @patch.object(MockModel, "predict", return_value={"test": 42.0})
    async def test_predict(self, mock_predict):
        forecaster = AsyncForecaster(MockModel, default_model_loader, model_count=1)
        mock_params = MagicMock()
        result = await forecaster.predict(mock_params)
        self.assertEqual(result, {"test": 42.0})

        with self.assertRaises(RuntimeError) as cm:
            while True:
                try:
                    forecaster.model_queue.get_nowait()
                except Exception:
                    break
            await forecaster.predict(mock_params)
        self.assertIn("No model available in queue", str(cm.exception))
