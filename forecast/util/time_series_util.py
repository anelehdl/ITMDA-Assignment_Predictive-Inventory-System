from pydoc import cli
import pandas as pd

__time_series_features = [
    "qty_lag1",
    "qty_lag5",
    "qty_lag10",
    "rolling_mean_3",
    "rolling_std_3",
    "rolling_mean_5",
    "rolling_std_5",
    "rolling_mean_10",
    "rolling_std_10",
    "days_since_client_purchase",
    "days_since_client_item_purchase",
]


def get_client_item_time_series_features(
    df: pd.DataFrame, client_name: str, item_code: str
):
    missing_features = [col for col in ["cust_id", "item"] if col not in df.columns]
    if missing_features:
        raise ValueError(f"Missing required features in dataframe: {missing_features}")

    group = df[(df["cust_id"] == client_name) & (df["item"] == item_code)]

    if group.empty:
        raise ValueError(f"No data for client={client_name} and item={item_code}")

    missing_features = [col for col in __time_series_features if col not in df.columns]
    if missing_features:
        raise ValueError(f"Missing required features in dataframe: {missing_features}")

    latest_features = group[__time_series_features].iloc[-1]
    return latest_features.to_dict()
