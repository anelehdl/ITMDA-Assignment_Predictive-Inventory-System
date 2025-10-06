from abc import ABC, abstractmethod
from typing import Callable, Any, Optional
from pathlib import Path
import numpy as np
import pandas as pd


class ParameterAdaptor(ABC):
    def __init__(self, features: list[str]):
        self.features: list[str] = features
        self.input: pd.DataFrame = pd.DataFrame()

    def __validate_rawdata(self, raw_data: pd.DataFrame):
        missing = [col for col in self.features if col not in raw_data.columns]
        assert not missing, f"Missing required columns: {missing}"

    def to_dataframe(self, raw_data={}) -> pd.DataFrame:
        if isinstance(raw_data, pd.DataFrame):
            self.__validate_rawdata(raw_data)
            return raw_data

        df: pd.DataFrame = pd.DataFrame(raw_data)
        self.__validate_rawdata(raw_data)
        return df

    @abstractmethod
    def transform(self, parameters):
        pass

    def parameters(self) -> pd.DataFrame:
        return self.input


class ForecastModel(ABC):
    @abstractmethod
    def load(self, path: str | Path, loader: Callable[[str | Path], Any] | None = None):
        pass

    @abstractmethod
    def predict(self, parameters: ParameterAdaptor) -> dict[str, float]:
        pass


class Forecaster:
    def __init__(self, model: ForecastModel):
        self.model: ForecastModel = model

    def predict(self, parameters: ParameterAdaptor) -> dict[str, float]:
        return self.model.predict(parameters)
