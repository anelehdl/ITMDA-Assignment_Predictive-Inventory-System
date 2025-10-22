from fastapi import FastAPI, HTTPException, Request
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
import logging
import os
from datetime import date
from prediction_requests import ClientItemPRequest


class PredictServiceEnviroment:
    def __init__(self) -> None:
        #used to find specific models in model directory
        self.predict_horizon: int = int(os.environ.get("PHORIZON", 1))


settings = ServiceEnvironment()
forecast_settings = PredictServiceEnviroment()
logging.basicConfig(
    level=logging.INFO,
    filename="prediction_service.log",
    filemode="a",
    format='%(asctime)s - %(levelname)s - %(message)s',  
    datefmt='%Y-%m-%d %H:%M:%S'  
)

logger = logging.getLogger(f"{__name__}_{forecast_settings.predict_horizon}")
forecaster = AsyncForecaster(
    DirectQuantileForecaster,
    loader=lgb_loader,
    model_name=f"Qhorizon{forecast_settings.predict_horizon}", #name of model e.g "Qhorizon10" where model on disk is "Qhorizon10_q10" for quantile 10
    quantiles=[10, 50, 70, 90],
)
# path for DirectQuantileForecaster should be "models/quantile_forecast"
forecaster.load(settings.models_dir / "quantile_forecast")
app = FastAPI(title=settings.service_name)


@app.on_event("startup")
def startup_event():
    
    #consul registration
    reg_id = register_service(
        name=f"{settings.service_name}",
        #using service_addr for address given by docker
        host=settings.service_addr,
        port=settings.port,
        unique_id=1,
        tags=[f"h{forecast_settings.predict_horizon}"],
        consul_params={
            "host": settings.consul_host,
            "port": settings.consul_port
        }
    )

    logging.info(f"Prediction Service {settings.service_name} registered at id: {reg_id} on {settings.service_addr}:{settings.port}")


@app.get("/health")
def health():
    return 200


def get_data_service():
    services = get_services(
        "data_service",
        consul_host=settings.consul_host,
        consul_port=int(settings.consul_port),
    )

    if not services:
        logger.warning("Data service unavailable")
        raise HTTPException( status_code=503, detail=f"No healthy instances of data service")

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
async def forecast_endpoint(prediction: ClientItemPRequest, request: Request):
    """Endpoint to transform and prepare data for the forecast prediction.
    """

    logger.info(f"Request:/predict   ADDRESS:{request.client.host}")
    try:
        #For easier feature extraction and calculation, data is obtained from a data service
        cached_features = get_cached_features(prediction.client_name, prediction.item)

        x_input = ClientItemAdaptor()
        params = {
            "item": prediction.item,
            "date": date.today(),
            "cust_code": prediction.customer_code,
            "cust_id": prediction.client_name,
            "price": prediction.price,
            "region": prediction.region,
            "area": prediction.area,
            "currency": prediction.currency,
            **cached_features,
        }

        x_input.transform(params)

        result = await forecaster.predict(x_input)
        return result
    
    except HTTPException as e:
        raise HTTPException(
            status_code = e.status_code,
            detail= {
                "error": "Unable to recevie features from data service",
                "reason": e.detail

            }
        )

    except Exception as e:
        logger.warning(f"Request: /predict  unknown error: {repr(e)}")
        raise HTTPException(status_code=500, detail=repr(e))
