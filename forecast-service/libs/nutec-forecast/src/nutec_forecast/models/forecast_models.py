from pathlib import Path

from typing import Callable, Any 
from typing import override
import joblib
import pandas as pd
import numpy as np

from nutec_forecast.models import ParameterAdaptor, ForecastModel
import lightgbm as lgb


class ClientItemAdaptor(ParameterAdaptor):
    """Adaptor for client X item forecasting input
    
    Specific Adaptor for client x item LightGBM models
    """
    def __init__(self):
        #features of very specific to the input of the model. 
        #As long as the model uses the same input features, parameters can be used.
        required_features = [
            "item",
            "date",
            "cust_id",
            "cust_code",
            "price",
            "region",
            "area",
            # time series features if not sent from api should be calculated from the data file (should be done outside class)
            "qty_lag1",
            "qty_lag5", #data is represented as only business days as no sales are done over the weekend. 1 week is 5 days instead of 7
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
    def transform(self, parameters: dict[str, Any] | pd.DataFrame):
        df = self.to_dataframe(parameters)

        df["date"] = pd.to_datetime(df["date"])

        #category related features (also known as dummy variables in other ML/DL methods) 
        df["item"] = df["item"].astype("category")
        df["region"] = df["region"].astype("category")
        df["area"] = df["area"].astype("category")
        df["cust_id"] = df["cust_id"].astype("category")
        df["cust_code"] = df["cust_code"].astype("category")
        df["category"] = (df["color"] + "_" + df["container"]).astype("category")
        df["color"] = df["color"].astype("category")
        df["container"] = df["container"].astype("category")
        df["currency"] = df["currency"].astype("category")

        #date related features
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

        # specificaly set which features that are allowed to be extracted fo forecasters
        # TODO:Find a better alternative to set which features to extract over using literals
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
        """Ininitializer for Forecaster

        Ininitialize the forecaster to be loaded for quantile predictions
        Args:
            model_name: the name of models to be loaded where the name of the model on disk is "{model_name}_q{quantile} for each quantile in list"
            quantiles: the set of quantiles the models to be loaded and predict
            horizon: the specific horizon the model will predict
        """
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
        Args:
            path: the directory where models are stored
            loader: a callable function that handles the loading of the specific model_name.
            Note* a model name is passed but file extension is left out explicitly to allow the loader to handle the types of models loaded
        """
        if loader is None:
            loader = lgb_loader
        path = Path(path)
        for key in self.models:
            self.models[key] = loader(path / f"{self.model_name}_{key}")

    @override
    def predict(self, parameters: ParameterAdaptor) -> dict[str, Any]:
        """ Predicts output for parameters

        Quantiles use multiple loaded models (model per quantile) to predict output at each specified quantile 
        Args:
            parameters: Parameter set that matches the input requirements of the model loaded from disk

        Returns:
            A dictionary set of Y outputs from loaded models for each quantile
            example:
                {
                "q10": 1.9,
                "q20": 2.3
                }
        """
        features = parameters.parameters()
        quant_predictions: dict[str, float] = {}

        for q_str, model in self.models.items():
            #for now only predicts 1 row of input
            #TODO: Remove when multiple input rows are capable without errors
            quant_predictions[q_str] = model.predict(features)[0]

        for key, q in quant_predictions.items():
            #model output is always in log transform so reverse operation is needed
            quant_predictions[key] = np.expm1(q)

        return quant_predictions
