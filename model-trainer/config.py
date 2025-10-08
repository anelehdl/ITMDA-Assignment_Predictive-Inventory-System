from pydantic import Field
from pydantic_settings import BaseSettings
from pathlib import Path
import json


class AppSettings(BaseSettings):
    model_export_dir: Path = Field(default=Path.cwd() / "models", env="MOD_EXPORT")
    data_export_dir: Path = Field(default=Path.cwd() / "models", env="DATA_EXPORT")
    train_hardware_pref: str = Field(default="cpu", env="TRAIN_DEVICE")
    log_level: str = Field(default="INFO", env="LOG_LEVEL")

    @classmethod
    def from_json(cls, path: str | Path = "config.json"):
        data = {}
        path = Path(path)

        if path.exists():
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)

        return cls(**data)
