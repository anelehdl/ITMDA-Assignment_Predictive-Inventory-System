from pathlib import Path
from service_env import (
    ServiceEnviroment,
    get_services,
)
from fastapi import FastAPI, HTTPException

import consul

service_id = 10
settings = ServiceEnviroment()
app = FastAPI(title=settings.service_name)


# get the specific prediction model based on tag e.g h1 tag to get horizon=1 model.
def get_model_service_owner(model_tag):
    services = get_services("predict-service", tag=model_tag)
    if not services:
        raise HTTPException(f"No healthy instances of prediction service [{model_tag}]")
    return services[0]
