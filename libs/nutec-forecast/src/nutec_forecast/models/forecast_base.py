"""Base Classes for forecasting models


"""


from abc import ABC, abstractmethod
from typing import Callable, Any, Optional, Dict
from pathlib import Path
import numpy as np
import pandas as pd


class ParameterAdaptor(ABC):
    """Parameter base class used as input for forecasters

    Forecaster models require a set of X inputs to produce a set of Y outputs. Because model input requirements
    can differ between models, users are required to implement a and adaptor to produce the correct X inputs for a model

    Usage:
    class MyAdoptor(ParameterAdaptor):
        
        def __init__(self):
            required_features = ["name", "age"]
            super().__init__(required_features)
    params = {
    "name": "john"
    "age": 42
    }
    adaptor = MyAdoptor():
    adaptor.transform(params)
    forecaster.predict(adaptor)
    """
    def __init__(self, features: list[str]):
        """Ininitializer for base class
        
        Parameters are a set of X features and requires at least 1 input.

        Args:
            features: The required features/keys/columns the passed transform parameters must have

        Raises:
            ValueError: Requires miniumn of 1 feature/key/column 
        """


        if 0 == len(features):
            raise ValueError("There must be minimun 1 required feature")

        self.features: list[str] = features
        self.input: pd.DataFrame = pd.DataFrame()

    def __validate_rawdata(self, raw_data: pd.DataFrame):
        missing = [col for col in self.features if col not in raw_data.columns]
        if missing:
            raise ValueError(f"Missing required columns: {missing}")

    def to_dataframe(self, raw_data: Dict[str, Any] | pd.DataFrame) -> pd.DataFrame:
        """ Transform input into dataframe
        
        A utility method to transform input parameters into a dataframe required for forecasters
        
        Args:
            raw_data: Dictonary or DataFrame holding the set of X inputs to be transformed

        Raises:
            ValueError: raw_data does not contain the required_features set by initializer
        """

        if isinstance(raw_data, pd.DataFrame):
            self.__validate_rawdata(raw_data)
            return raw_data

        df = pd.DataFrame([raw_data])
        self.__validate_rawdata(df)
        return df

    @abstractmethod
    def transform(self, parameters: Dict[str, Any] | pd.DataFrame):
        """ Transforms parameters into suitable format for forecaster
            
        Args:
            parameters: Dictonary or DataFrame holding the set of X inputs to be transformed

        """
        pass

    @abstractmethod
    def parameters(self) -> pd.DataFrame:
        """ Expected to return X dataset for forecaster

        A forecaster expects X inputs to produce Y outputs. Forecasters can retrieve inputs from this interface

        Returns:
            DataFrame object with suitable data for forecaster
        """
        pass


class ForecastModel(ABC):
    @abstractmethod
    def load(self, path: str | Path, loader: Callable[[str | Path], Any] | None = None):
        """ Loads model into forecaster
        
        Loads stored model from disk

        Args:
            path: Path for models. Forecasting obejects may have there own requirements for the path, see documentation load
            for each forecaster class
            loader: The loader used in loading model objects from disk
        """
        pass

    @abstractmethod
    def predict(self, parameters: ParameterAdaptor) -> dict[str, float]:
        """Makes a prediction output given input parameters

        Forecaster will use loaded model to make prediction given X input

        Args:
            parameters: The X input parameters used for the Forecaster.

        Returns:
            A dictonary str key value pair for each output from model 
            example
            {
                "q10": 5,
                "q20": 10
            }
        """
        pass
