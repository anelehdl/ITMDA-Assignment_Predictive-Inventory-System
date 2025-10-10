from .models import ForecastModel, ParameterAdaptor

from pathlib import Path
from typing import Callable, List, Type, Any
import asyncio
from asyncio import Queue


class AsyncForecaster:
    def __init__(
        self,
        model_cls: Type,
        loader: Callable[[str | Path], Any],
        model_count: int = 3,
        *args,
        **kwargs,
    ):
        self.count: int = model_count
        self.model_queue: Queue = Queue(maxsize=model_count)
        self.model_loader = loader
        self.loadable_models: List[Any] = []
        for _ in range(model_count):
            model = model_cls(*args, **kwargs)
            self.model_queue.put_nowait(model)
            self.loadable_models.append(model)

    def load(self, path: str | Path):
        for model in self.loadable_models:
            model.load(path, self.model_loader)

    async def predict(
        self, params: ParameterAdaptor, timeout: float = 5.0
    ) -> dict[str, Any] | None:
        try:
            model = await asyncio.wait_for(self.model_queue.get(), timeout=timeout)
        except asyncio.TimeoutError:
            raise RuntimeError("No model available in queue")

        try:
            result = model.predict(params)
        finally:
            await self.model_queue.put(model)

        return result
