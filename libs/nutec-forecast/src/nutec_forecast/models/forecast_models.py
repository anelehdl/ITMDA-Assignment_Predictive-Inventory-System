from pathlib import Path

from typing import Callable, Any
from typing import override
import joblib
import pandas as pd
import numpy as np

from nutec_forecast.models import ParameterAdaptor, ForecastModel
import lightgbm as lgb


class ClientItemAdaptor(ParameterAdaptor):
    def __init__(self):
        required_features = [
            "item",
            "date",
            "cust_id",
            "cust_code",
            "price",
            "region",
            "area",
            # time series features if not sent from api should be calculated from the pickle data file (should be done outside class)
            "qty_lag1",
            "qty_lag5",
            "qty_lag10",
            "rolling_mean_3",
            "rolling_std_3",
            "rolling_mean_5",
            "rolling_std_5",
            "rolling_mean_10",
            "rolling_std_10",
            "days_since_client_purchase",
            "days_since_client_item_purchase",
        ]
        super().__init__(required_features)

    @override
    def transform(self, parameters):
        df = self.to_dataframe(parameters)
        df["date"] = pd.to_datetime(df["date"])
        df["item"] = df["item"].astype("category")
        df["region"] = df["region"].astype("category")
        df["area"] = df["area"].astype("category")
        df["cust_id"] = df["cust_id"].astype("category")
        df["cust_code"] = df["cust_code"].astype("category")
        df["category"] = (df["color"] + "_" + df["container"]).astype("category")
        df["color"] = df["color"].astype("category")
        df["container"] = df["container"].astype("category")
        df["currency"] = df["currency"].astype("category")

        df["dayofweek"] = df["date"].dt.dayofweek
        df["dayofweek_sin"] = np.sin(2 * np.pi * df["dayofweek"] / 7)
        df["dayofweek_cos"] = np.cos(2 * np.pi * df["dayofweek"] / 7)
        df["month"] = df["date"].dt.month
        df["month_sin"] = np.sin(2 * np.pi * df["month"] / 12)
        df["month_cos"] = np.cos(2 * np.pi * df["month"] / 12)
        df["dayofyear"] = df["date"].dt.dayofyear
        df["dayofyear_sin"] = np.sin(2 * np.pi * df["dayofyear"] / 365)
        df["dayofyear_cos"] = np.cos(2 * np.pi * df["dayofyear"] / 365)
        df["day"] = df["date"].dt.day
        df["day_sin"] = np.sin(2 * np.pi * df["day"] / 30)
        df["day_cos"] = np.cos(2 * np.pi * df["day"] / 30)
        df["year"] = df["date"].dt.year
        df["quarter"] = df.date.dt.quarter
        df["quarter_sin"] = np.sin(2 * np.pi * df["quarter"] / 4)
        df["quarter_cos"] = np.cos(2 * np.pi * df["quarter"] / 4)

        df["price"] = np.log1p(df["price"])
        self.input = df
        self.forecast_features = [
            "cust_code",
            "price",
            "cust_id",
            "item",
            "currency",
            "region",
            "category",
            "day_sin",
            "day_cos",
            "month",
            "dayofweek_cos",
            "dayofweek_sin",
            "dayofyear_sin",
            "dayofyear_cos",
            "quarter_sin",
            "quarter_cos",
            "qty_lag1",
            "qty_lag5",
            "qty_lag10",
            "qty_lag20",
            "rolling_mean_3",
            "rolling_std_3",
            "rolling_mean_5",
            "rolling_std_5",
            "rolling_mean_10",
            "rolling_std_10",
            "days_since_client_purchase",
            "days_since_client_item_purchase",
        ]

    @override
    def parameters(self) -> pd.DataFrame:
        return self.input[self.forecast_features]


def default_model_loader(path: str | Path):
    import joblib

    return joblib.load(path)


def lgb_loader(path: str | Path):
    file = Path(path).with_suffix(".json")
    return lgb.Booster(model_file=file)


def load_dataset(path: str | Path) -> pd.DataFrame:
    df: pd.DataFrame = pd.read_parquet(path)
    return df


class DirectQuantileForecaster(ForecastModel):
    def __init__(self, model_name: str, quantiles: list[int], horizon: int = 1):
        super().__init__()
        self.model_name: str = model_name
        self.models: dict[str, Any] = {}
        self.quantiles: list[int] = quantiles
        self.horizon = horizon
        self.__create_model_names()

    def __create_model_names(self):
        self.models = {f"q{q}": None for q in self.quantiles}

    @override
    def load(self, path: str | Path, loader: Callable[[str | Path], Any] | None = None):
        """
        Loads all quantile models under directory in path
        Parameters:
            path: the directory where models are stored
            loader: a callable function that handles the loading of the specific model_name.
            Note a model name is passed but file extenstion is left out explixitly to allow the loader to handle the types of models loaded
        """
        if loader is None:
            loader = lgb_loader
        path = Path(path)
        for key in self.models:
            self.models[key] = loader(path / f"{self.model_name}_{key}")

    @override
    def predict(self, parameters: ParameterAdaptor) -> dict[str, float]:
        features = parameters.parameters()
        quant_predictions: dict[str, float] = {}

        for q_str, model in self.models.items():
            quant_predictions[q_str] = model.predict(features)[0]

        for key, q in quant_predictions.items():
            quant_predictions[key] = np.expm1(q)

        return quant_predictions
