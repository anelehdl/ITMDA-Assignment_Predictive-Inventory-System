from pydantic_settings import BaseSettings, SettingsConfigDict


class PackageSettings(BaseSettings):
    models_dir: str = "./predict/models"
    log_level: str = "INFO"

    model_config = SettingsConfigDict(env_prefix="PRD")


settings = PackageSettings()
