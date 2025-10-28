
import pytest
import requests

BASE_PREDICT = "http://localhost:8620"
BASE_ITEM = "http://localhost:8520"


def test_post_predict():
    payload = {
        "item": "4750619",
        "client_name": "V007 - SO",
        "customer_code": "V007",
        "region": "BRAZIL",
        "area": "SO",
        "price": 199.99,
        "currency": "USD"
    }  

    response = requests.post(f"{BASE_PREDICT}/predict/h5", json=payload)
    
    # contract test
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, dict)
    assert "q10" in data  


def test_get_models():
    response = requests.get(f"{BASE_PREDICT}/models")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, dict)

    # contract test
    if data:
        assert "models" in data
        assert  isinstance(data["models"], list)


def test_get_item_features():
    item_id = 4750520
    response = requests.get(f"{BASE_ITEM}/item/{item_id}")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, dict)

    # contract test
    assert "container" in data
    assert "color" in data


def test_post_time_series_features():
    payload = {
        "item": "4750828",
        "client_name": "L001 - SA"
    }  
    contract_features = {
        "qty_lag1",
        "qty_lag5",
        "qty_lag10",
        "qty_lag20",
        "rolling_mean_3",
        "rolling_std_3",
        "rolling_mean_5",
        "rolling_std_5",
        "rolling_mean_10",
        "rolling_std_10",
        "days_since_client_purchase",
        "days_since_client_item_purchase"
}
    response = requests.post(f"{BASE_ITEM}/time-features", json=payload)
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, dict)
    
    if data:
        # contract test
        assert set(data.keys()) == contract_features


