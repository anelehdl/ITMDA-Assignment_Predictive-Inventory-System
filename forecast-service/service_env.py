from fastapi import HTTPException
from pydantic_settings import BaseSettings
import consul
import os

from pydantic import Field
from pathlib import Path
from typing import Any, Dict

PROJECT_ROOT = Path(__file__).parent


class ServiceEnvironment:
    """ Enviroment variables for service

    Contains all the needed variables for service hosting, directory discovery
    and service discovery from consul

    Example:
    settings = ServiceEnvironment()
    host = settings.host
    """

    #TODO: Replace variables with property so no need to intitialize each time
    def __init__(self):
        self.host = os.environ.get("HOST", "localhost")
        self.port = os.environ.get("PORT", "8420")
        self.consul_host = os.environ.get("CONSUL_HOST", "localhost")
        self.consul_port = os.environ.get("CONSUL_PORT", 8500)
        self.service_name = os.environ.get("PNAME", "predict-service")
        self.service_addr = os.environ.get("SERVICE_ADDRESS", "0.0.0.0")
        self.data_dir = Path(os.environ.get("DATA_DIR", PROJECT_ROOT / "data"))
        self.models_dir = Path(os.environ.get("MODEL_DIR", PROJECT_ROOT / "models"))


def register_service(
    name,
    host,
    port,
    unique_id=1,
    tags=[],
    consul_params: Dict[str, Any] | None = None,
):
    """Utiliy function for Consul registering.

    Registers the current service to a consul service discovery.

    Args:
        name: The name for the service for discovery with names.
        host: Service host.
        port: Service port.
        unique_id: id to randomize service with same name. (incase host and port is the same although this should be impossible)
        tags: a list of tags to further identify services with the same name.
        consul_params: parameters to connect to consu.l 

    Returns:
        Returns the registered id.
    """

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
    """Retrieves the service from consul.

    Registered services can be located from consul matching name pattern and specified tag.

    Args:
        service_name: Name of the service.
        tag: Tags to narrow search with multiple same service names
        consul_host: Host of consul service discovery
        consul_port: Port of consul service discovery

    Returns:
        Returns a list of services that match search parameters 
    """
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
