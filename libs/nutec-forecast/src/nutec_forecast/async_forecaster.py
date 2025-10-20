from .models import ForecastModel, ParameterAdaptor

from pathlib import Path
from typing import Callable, List, Type, Any
import asyncio
from asyncio import Queue


class AsyncForecaster:
    """
    A threadsafe forecaster wrapper for forecasting models.

    Creates multiple forecast objects for a single forecaster for multithreaded and async prediction. 

    Usage:

    forecaster = AsyncForecaster(MyForecaster, my_loader, 5)
    forecaster.load("/path/to/model")
    results = forecaster.predict(my_parameters)
    
    """
    def __init__(
        self,
        model_cls: Type,
        loader: Callable[[str | Path], Any],
        model_count: int = 3,
        *args,
        **kwargs,
    ):
        """Initializes the instance on the specific class forecaster.
        
        Args:
            model_cls: The specifc class that forecaster objects will be created from.
            loader: The loader used in loading model objects from disk
            model_count: How many forecaster objects to create for queued prediction
            args: Args for the created Class initializing
            kwargs: Kwargs for the created Class initializing 

        
        """
        self.count: int = model_count
        self.model_queue: Queue = Queue(maxsize=model_count)
        self.model_loader = loader
        
        # load needs to be called first before prediction. No easy way to iterate of queue without pull and pushing.
        # Use second list to seperate load and model access
        self.loadable_models: List[Any] = []
        for _ in range(model_count):
            model = model_cls(*args, **kwargs)
            self.model_queue.put_nowait(model)
            self.loadable_models.append(model)

    def load(self, path: str | Path):
        """Loads all queued forecasters models

        Using the initialized loader, all models that are registered to load with be loaded in into each forecaster object.
        Calling load again will simply reload the models from disk

        Args:
            path: Path for models. Forecasting obejects may have there own requirements for the path, see documentation load
            for each forecaster class
        """
        for model in self.loadable_models:
            model.load(path, self.model_loader)

    async def predict(
        self, params: ParameterAdaptor, timeout: float = 5.0
    ) -> dict[str, Any] | None:
        """Makes a prediction output given input parameters

        Forecaster will fetch an avaialbe forecast model for prediction and make a requests

        Args:

            params: The X input parameters used for the Forecaster class.
        
        Raises:
            RuntimeError: Took to long to find available model and timesout request

        Returns:
            A dictonary str key value pair for each output from model 
            example
            {
                "q10": 5,
                "q20": 10
            
            }
        
        """

        try:
            # dont want to wait for ever prefer asyncio io timeout of queue timeout to ensure coroutine is canceled
            model = await asyncio.wait_for(self.model_queue.get(), timeout=timeout)
        except asyncio.TimeoutError:
            raise RuntimeError("No model available in queue")

        try:
            result = model.predict(params)
        finally:
            await self.model_queue.put(model)

        return result
