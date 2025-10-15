from fastapi import HTTPException
from pydantic_settings import BaseSettings
import consul
import os

from pydantic import Field
from pathlib import Path
from typing import Any, Dict

PROJECT_ROOT = Path(__file__).parent


class ServiceEnviroment(BaseSettings):
    host: str = Field(default="localhost", env="HOST")
    port: str = Field(default="8420", env="PORT")

    service_name: str = Field(default="predict-service", env="PNAME")
    data_dir: Path = Field(default=PROJECT_ROOT / "data", env="DATA_DIR")
    models_dir: Path = Field(default=PROJECT_ROOT / "models", env="MODEL_DIR")

    # data_host: str = Field(default="localhost", env="DHOST")
    # data_port: str = Field(default="8520", env="DPORT")
    # data_service: str = Field(default="data-service", env="DNAME")


def register_service(
    name,
    host,
    port,
    unique_id=1,
    tags=[],
    consul_params: Dict[str, Any] | None = None,
):
    if not consul_params:
        consul_params = {
            "host": os.getenv("CONSUL_HOST", "localhost"),
            "port": int(os.getenv("CONSUL_PORT", 8500)),
        }

    c = consul.Consul(**consul_params)
    service_id = f"{name}_{port}_{unique_id}"

    c.agent.service.register(
        name=name,
        service_id=service_id,
        address=host,
        port=int(port),
        check=consul.Check.http(f"http://{host}:{port}/health", interval="10s"),
        tags=tags,
    )
    return service_id


def get_services(service_name, tag=None, consul_host="localhost", consul_port=8500):
    import requests

    try:
        url = f"http://{consul_host}:{consul_port}/v1/catalog/service/{service_name}"

        if tag:
            url += f"?tag={tag}"

        response = requests.get(url)
        response.raise_for_status()
        services = response.json()
        return services

    except requests.exceptions.HTTPError as e:
        raise HTTPException(
            status_code=e.response.status_code,
            detail=f"Service discovery error: {e.response.text}",
        )

    except requests.exceptions.RequestException as e:
        raise HTTPException(
            status_code=503, detail=f"Could not reach service discovery: {str(e)}"
        )


def service_host_port(service: Dict[str, Any]):
    return service["ServiceAddress"], service["ServicePort"]
