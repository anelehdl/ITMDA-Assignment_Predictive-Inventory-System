from .models import ForecastModel, ParameterAdaptor

from typing import Type, Any
import asyncio
from asyncio import Queue


class AsyncForcaster:
    def __init__(self, model_cls: Type, model_count: int = 3, *args, **kwargs):
        self.count: int = model_count
        self.model_queue: Queue = Queue(maxsize=model_count)

        for _ in range(model_count):
            model = model_cls(*args, **kwargs)
            self.model_queue.put_nowait(model)

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
