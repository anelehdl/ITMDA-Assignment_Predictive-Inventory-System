from abc import ABC, abstractmethod
import pandas as pd

class ParameterAdaptor(ABC):

    def __init__(self, features=[])
        self.features = features
        sefl.input = pd.Dataframe()

    def __validate_rawdata(raw_data):
        missing = [col for col in self.features if col not in raw_data.columns]
        assert not missing, f"Missing required columns: {missing}"
    
    
    def __to_dataframe(self, raw_data = {}) -> pd.Dataframe:
        if raw_data isintance(raw_data, pd.Dataframe)
            __validate_rawdata(raw_data)
            return raw_data

        df = pd.Dataframe(raw_data)
        __validate_rawdata(raw_data)

        return df
    
    @abstractmethod
    def transform(self, parameters):
        pass
    
    @abstractmethod
    def recurse_update(self, prediction):
        pass

    def parameters(self) -> pd.Dataframe:
        return self.input


class ForecastModel(ABC):

    @abstarctmethod
    def load(self, path: str)
        pass

    @abstractmethod
    def predict(self, x_params: ParameterAdaptor, steps=1):
        pass


class Forecaster:


    def __init__(self, model):
        self.model = model

    
    def predict(self, x_params: ParameterAdaptor, steps=1):
        return self.model.predict(x_params, steps)




