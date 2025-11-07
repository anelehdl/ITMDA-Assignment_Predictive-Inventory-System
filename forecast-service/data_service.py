from pathlib import Path
from service_env import ServiceEnvironment, register_service
from typing import Dict, Any

from nutec_forecast.util.time_series_util import (
    get_client_item_time_series_features,
    get_item_info,
)
from fastapi import FastAPI, HTTPException, Request
from pydantic import BaseModel

import dataclasses
import pandas as pd
import os
import logging
from dataclasses import dataclass


#ensure data is read only from dataset
@dataclass(frozen=True)
class CachedFeatures:
    data: pd.DataFrame


class FeatureRequest(BaseModel):
    item: str
    client_name: str

logging.basicConfig(
    level=logging.INFO,
    filename="data_service.log",
    filemode="a",
    format='%(asctime)s - %(levelname)s - %(message)s',  
    datefmt='%Y-%m-%d %H:%M:%S'  
)
logger = logging.getLogger(__name__)
service_id = 42
settings = ServiceEnvironment()
local_data = CachedFeatures(
    data=pd.read_feather(Path(settings.data_dir) / "cached_features.feather")
)
app = FastAPI(title=settings.service_name)

@app.on_event("startup")
def startup_event():
    
    #consul registration
    reg_id = register_service(
        name=f"{settings.service_name}",
        #using service_addr for address given by docker
        host=settings.service_addr,
        port=settings.port,
        consul_params={
            "host": settings.consul_host,
            "port": settings.consul_port
        }
    )
    logging.info(f"Data Service registered at id: {reg_id}")
    



@app.get("/health")
def health():
    return 200


@app.post("/time-features", response_model=Dict[str, Any])
async def get_cached_time_features(request: Request, feature: FeatureRequest):
    """Endpoint to get time series related features for forecasting. 
    """
    
    logger.info(f"Request:/time-features ADDRESS:{request.client.host}")
    try:
        features = get_client_item_time_series_features(
            local_data.data, feature.client_name, feature.item
        )
    except ValueError as ve:
        raise HTTPException(status_code=404, detail={"error": str(ve)})
    return features


@app.get("/item/{item_id}")
async def get_item(item_id: str, request: Request):
    """ Endpoint to get category related features for item.
    """
    logger.info(f"Request:/item  value:{item_id} ADDRESS:{request.client.host}")
    try:
        info = get_item_info(local_data.data, item_id)
    except ValueError as ve:
        raise HTTPException(status_code=404, detail={"error": str(ve)})
    return info
