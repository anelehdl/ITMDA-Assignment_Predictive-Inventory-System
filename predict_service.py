from pkgutil import get_data
from turtle import resetscreen
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field
from pydantic_settings import BaseSettings
from typing import Dict, Any, List
from nutec_forecast.models import (
    ClientItemAdaptor,
    DirectQuantileForecaster,
    lgb_loader,
)
from nutec_forecast import AsyncForecaster
from nutec_forecast.util.time_series_util import get_client_item_time_series_features
from pathlib import Path
from service_env import ServiceEnviroment, register_service

import consul
import random
import requests
from datetime import date


class PredictServiceEnviroment(BaseSettings):
    direct_quantiles: List[int] = Field(default_factory=list, env="QUANTILES")


settings = ServiceEnviroment()
forecast_settings = PredictServiceEnviroment()

forecaster = AsyncForecaster(
    DirectQuantileForecaster,
    loader=lgb_loader,
    model_name="Qhorizon1",
    quantiles=[10, 50, 70, 90],
)
forecaster.load(settings.models_dir / "quantile_forecast")
register = consul.Consul(host="localhost", port=8500)

app = FastAPI(title=settings.service_name)


class PredictRequest(BaseModel):
    item: str
    client_name: str
    customer_code: str
    region: str
    area: str
    price: float
    currency: str


@app.on_event("startup")
def startup_event():
    reg_id = register_service(
        name=f"predict-{settings.service_name}",
        host=settings.host,
        port=settings.port,
        unique_id=1,
    )


@app.get("/health")
def health():
    return 200


# TODO: Fix service discovery for data-service
def get_data_service():
    # services = register.health.service(settings.data_service, passing=True)[1]

    # if not services:
    #     raise HTTPException(f"No healthy instances of data service {services}")

    # svc = random.choice(services)
    # address = svc["Service"]["Address"]
    # port = svc["Service"]["Port"]
    return f"http://localhost:8520"


def get_cached_features(client_id, item_id):
    service = get_data_service()
    endpoint = f"{service}/time-features"

    data = {"item": item_id, "client_name": client_id}
    resp_time = requests.post(endpoint, json=data)

    if 200 != resp_time.status_code:
        raise HTTPException(
            status_code=resp_time.status_code,
            detail={"error": "unable to retreive cached time features"},
        )
    resp_item = requests.get(f"{service}/item/{item_id}")

    if 200 != resp_item.status_code:
        raise HTTPException(
            status_code=resp_item.status_code,
            detail={"error": "unable to retreive cached item features"},
        )

    time_features = resp_time.json()

    item_features = resp_item.json()

    combined = {**time_features, **item_features}
    return combined


@app.post("/predict", response_model=Dict[str, Any])
async def forecast_endpoint(request: PredictRequest):
    """Endpoint that cleans and prepares the data for the forecaster."""
    try:
        # Calculate time-series features
        cached_features = get_cached_features(request.client_name, request.item)

        # Prepare parameters with ClientItemAdaptor
        adaptor = ClientItemAdaptor()
        params = {
            "item": request.item,
            "date": date.today(),
            "cust_code": request.customer_code,
            "cust_id": request.client_name,
            "price": request.price,
            "region": request.region,
            "area": request.area,
            "currency": request.currency,
            **cached_features,
        }

        adaptor.transform(params)

        result = await forecaster.predict(adaptor)
        return result
    except Exception as e:
        raise HTTPException(status_code=500, detail=repr(e))
