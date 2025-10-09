from pydantic_settings import BaseSettings
from pydantic import Field
from pathlib import Path


class PackageSettings(BaseSettings):
    model_dir: Path = Field(default=Path.cwd() / "predict" / "models", env="MOD_DIR")
    data_dir: Path = Field(default=Path.cwd() / "predict" / "data", env="DATA_DIR")
    log_level: str = Field(default="INFO", env="LOG_LEVEL")


settings = PackageSettings()
