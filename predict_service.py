from fastapi import FastAPI, HTTPException
from typing import Dict, Any
from nutec_forecast.models import (
    ClientItemAdaptor,
    DirectQuantileForecaster,
    lgb_loader,
)
from nutec_forecast import AsyncForecaster
from service_env import (
    ServiceEnvironment,
    register_service,
    get_services,
    service_host_port,
)

import random
import requests
import os
from datetime import date
from prediction_requests import ClientItemPRequest


class PredictServiceEnviroment:
    def __init__(self) -> None:
        self.predict_horizon: int = int(os.environ.get("PHORIZON", 1))


settings = ServiceEnvironment()
forecast_settings = PredictServiceEnviroment()

forecaster = AsyncForecaster(
    DirectQuantileForecaster,
    loader=lgb_loader,
    model_name=f"Qhorizon{forecast_settings.predict_horizon}",
    quantiles=[10, 50, 70, 90],
)
forecaster.load(settings.models_dir / "quantile_forecast")
app = FastAPI(title=settings.service_name)


@app.on_event("startup")
def startup_event():
    reg_id = register_service(
        name=f"{settings.service_name}",
        host=settings.service_addr,
        port=settings.port,
        unique_id=1,
        tags=[f"h{forecast_settings.predict_horizon}"],
    )


@app.get("/health")
def health():
    return 200


def get_data_service():
    services = get_services(
        "data_service",
        consul_host=settings.consul_host,
        consul_port=int(settings.consul_port),
    )
    print(services)

    if not services:
        raise HTTPException(f"No healthy instances of data service")

    svc = random.choice(services)
    host, port = service_host_port(svc)
    return f"http://{host}:{port}"


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
async def forecast_endpoint(request: ClientItemPRequest):
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
