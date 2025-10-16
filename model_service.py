from pathlib import Path
from typing import Dict, Any
from service_env import ServiceEnvironment, get_services, service_host_port
from prediction_requests import ClientItemPRequest
from fastapi import FastAPI, HTTPException

import requests

service_id = 10
settings = ServiceEnvironment()
app = FastAPI(title=settings.service_name)


# get the specific prediction model based on tag e.g h1 tag to get horizon=1 model.
def get_model_service_owner(model_tag):
    services = get_services(
        "predict_service",
        tag=model_tag,
        consul_host=settings.consul_host,
        consul_port=int(settings.consul_port),
    )
    if not services:
        raise HTTPException(f"No healthy instances of prediction service [{model_tag}]")
    return services[0]


def make_client_item_request(request, model_tag):
    try:
        body = request.model_dump()
        service = get_model_service_owner(model_tag)
        host, port = service_host_port(service)
        url = f"http://{host}:{port}/predict"
        response = requests.post(url, json=body)
        return response.json()
    except Exception as e:
        raise HTTPException(status_code=500, detail=repr(e))


@app.post("/predict/h1", response_model=Dict[str, Any])
async def predict_h1(request: ClientItemPRequest):
    return make_client_item_request(request, "h1")


@app.post("/predict/h5", response_model=Dict[str, Any])
async def predict_h5(request: ClientItemPRequest):
    return make_client_item_request(request, "h5")


@app.post("/predict/h10", response_model=Dict[str, Any])
async def predict_h10(request: ClientItemPRequest):
    return make_client_item_request(request, "h10")


@app.post("/predict/h20", response_model=Dict[str, Any])
async def predict_h20(request: ClientItemPRequest):
    return make_client_item_request(request, "h20")


# TODO: Replace with service discovery
@app.get("/models", response=Dict[str, Any])
async def get_models():
    models = [
        "h1",
        "h5",
        "h10",
        "h20",
    ]

    return {"models": models}
