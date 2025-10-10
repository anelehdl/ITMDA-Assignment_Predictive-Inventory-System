from pathlib import Path
from service_env import ServiceEnviroment, register_service
from typing import Dict, Any

from nutec_forecast.util.time_series_util import (
    get_client_item_time_series_features,
    get_item_info,
)
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

import dataclasses
import pandas as pd

from dataclasses import dataclass


@dataclass(frozen=True)
class CachedFeatures:
    data: pd.DataFrame


class FeatureRequest(BaseModel):
    item: str
    client_name: str


service_id = 42
settings = ServiceEnviroment()
settings.host = "localhost"
print(f"{settings.data_dir}/cached_features.feather")
local_data = CachedFeatures(
    data=pd.read_feather(Path(settings.data_dir) / "cached_features.feather")
)
app = FastAPI(title=settings.service_name)


@app.on_event("startup")
def startup_event():
    reg_id = register_service(
        name=settings.data_service,
        host=settings.host,
        port=settings.port,
        unique_id=service_id,
    )


@app.get("/health")
def health():
    return 200


@app.post("/time-features", response_model=Dict[str, Any])
async def get_cached_time_features(request: FeatureRequest):
    try:
        features = get_client_item_time_series_features(
            local_data.data, request.client_name, request.item
        )
    except ValueError as ve:
        raise HTTPException(status_code=404, detail={"error": str(ve)})
    return features


@app.get("/item/{item_id}")
async def get_item(item_id: str):
    try:
        info = get_item_info(local_data.data, item_id)
    except ValueError as ve:
        raise HTTPException(status_code=404, detail={"error": str(ve)})
    return info
