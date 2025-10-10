from pydantic import BaseSettings
import consul

from pydantic import BaseSettings, Field
from pathlib import Path
from typing import Any


class ServiceEnviroment(BaseSettings):
    host: str = Field(default="localhost", env="HOST")
    port: str = Field(default="8420", env="PORT")

    service_name: str = Field(default="predict-service", env="PNAME")
    data_dir: Path = Field(default="predict-service", env="DATA_DIR")

    data_host: str = Field(default="localhost", env="DHOST")
    data_port: str = Field(default="8520", env="DPORT")
    data_service: str = Field(default="forecast", env="DNAME")


def register_service(
    name, host, port, unique_id=1, consul_params: dict[str, Any] | None = None
):
    if not consul_params:
        consul_params = {"host": "localhost", "port": 8500}

    c = consul.Consul(**consul_params)
    service_id = f"{name}_{port}_{unique_id}"

    c.agent.service.register(
        name=name,
        service_id=service_id,
        address=host,
        port=port,
        check=consul.Check.tcp(host, port, interval="10s"),
    )
    return service_id
