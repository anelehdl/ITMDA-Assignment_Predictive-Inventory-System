from abc import ABC, abstractmethod
from pathlib import Path
from typing import Any
import pandas as pd


class Trainer(ABC):
    @abstractmethod
    def train(self, train_data: Any, validation_data: Any):
        pass

    @abstractmethod
    def export(self, path: str | Path):
        pass

    @abstractmethod
    def evaluate(self) -> dict[str, float]:
        pass


class DataPrep(ABC):
    @abstractmethod
    def transform(self, dataframe: pd.DataFrame):
        pass

    @abstractmethod
    def get_data(self) -> pd.DataFrame:
        pass

    @abstractmethod
    def train_set(self) -> pd.DataFrame:
        pass

    @abstractmethod
    def test_set(self) -> pd.DataFrame:
        pass
