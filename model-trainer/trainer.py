from typing import override
from .trainer_base import Dataset, Trainer
from .util import target_feature_split
from pathlib import Path
from typing import Any, Callable

import lightgbm as lgb
import joblib


class QuantileClientItemLGBM(Trainer):
    def __init__(
        self,
        name: str,
        features: list[str],
        target,
        caterory_features: list[str],
        quantiles: list[int] = [10, 50, 90],
        model_params={},
    ):
        self.name = name
        self.quantiles = quantiles
        self.features = features
        self.target = target
        self.cat_features = caterory_features
        self.models = {}
        self.model_params = (
            {
                "objective": "quantile",
                "metric": "rmse",
                "boosting_type": "gbdt",
                "learning_rate": 0.01,
                "num_leaves": 31,
                "verbose": -1,
                "seed": 42,
            }
            if model_params is None
            else model_params
        )

    @override
    def train(self, dataset: Dataset):
        X_train, y_train = target_feature_split(
            dataset.train_set(), self.features, self.target
        )
        X_test, y_test = target_feature_split(
            dataset.test_set(), self.features, self.target
        )

        lgb_train = lgb.Dataset(
            X_train, label=y_train, categorical_feature=self.cat_features
        )
        lgb_test = lgb.Dataset(
            X_test,
            label=y_test,
            reference=lgb_train,
            categorical_feature=self.cat_features,
        )
        for q in self.quantiles:
            self.model_params["alpha"] = q / 100
            self.models[q] = lgb.train(
                self.model_params,
                lgb_train,
                valid_sets=[lgb_train, lgb_test],
                valid_names=["train", "valid"],
                num_boost_round=1000,
            )

    @override
    def export(self, path: str | Path, dumper: Callable[[str | Path, Any], None]):
        export_folder = Path(path) / f"m_{self.name}"
        export_folder.parent.mkdir(parents=True, exist_ok=True)
        for key, model in self.models.items():
            ex_path = export_folder / f"model_{key}"
            dumper(ex_path, model)

        if self.features is not None:
            features_path = export_folder / f"{self.name}_features.joblib"
            joblib.dump(self.features, features_path)

    @override
    def evaluate(self) -> dict[str, float]:
        return {}
