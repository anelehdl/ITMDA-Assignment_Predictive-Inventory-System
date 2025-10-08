from abc import ABC, abstractmethod
from pathlib import Path
from typing import Any, Callable
import pandas as pd


class Dataset(ABC):
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


class Trainer(ABC):
    @abstractmethod
    def train(self, dataset: Dataset):
        pass

    @abstractmethod
    def export(self, path: str | Path, dumper: Callable[[str | Path, Any], None]):
        pass

    @abstractmethod
    def evaluate(self) -> dict[str, float]:
        pass
