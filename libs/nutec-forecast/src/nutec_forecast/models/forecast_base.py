from abc import ABC, abstractmethod
from typing import Callable, Any, Optional
from pathlib import Path
import numpy as np
import pandas as pd


class ParameterAdaptor(ABC):
    def __init__(self, features: list[str]):
        if 0 == len(features):
            raise ValueError("There must be minimun 1 required feature")

        self.features: list[str] = features
        self.input: pd.DataFrame = pd.DataFrame()

    def __validate_rawdata(self, raw_data: pd.DataFrame):
        missing = [col for col in self.features if col not in raw_data.columns]
        if missing:
            raise ValueError(f"Missing required columns: {missing}")

    def to_dataframe(self, raw_data={}) -> pd.DataFrame:
        if isinstance(raw_data, pd.DataFrame):
            self.__validate_rawdata(raw_data)
            return raw_data

        df = pd.DataFrame([raw_data])
        self.__validate_rawdata(df)
        return df

    @abstractmethod
    def transform(self, parameters):
        pass

    @abstractmethod
    def parameters(self) -> pd.DataFrame:
        pass


class ForecastModel(ABC):
    @abstractmethod
    def load(self, path: str | Path, loader: Callable[[str | Path], Any] | None = None):
        pass

    @abstractmethod
    def predict(self, parameters: ParameterAdaptor) -> dict[str, float]:
        pass
