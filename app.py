from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Dict, List, Any
from nutec_forecast.models import ClientItemAdaptor, DirectQuantileForecaster
from nutec_forecast.util.time_series_util import get_client_item_time_series_features
from pymongo import MongoClient
from sklearn.preprocessing import MinMaxScaler
import pandas as pd
import numpy as np
from pathlib import Path
from datetime import datetime
from config import settings  # From forecast-models/src/nutec_forecast/config.py

app = FastAPI(title="Forecast Service")

# MongoDB connection (must still update URI)
MONGO_URI = ""
DB_NAME = "nutec_inventory"
client = MongoClient(MONGO_URI)
db = client[DB_NAME]

class PredictRequest(BaseModel):
    client_name: str
    customer_code: str
    predict_horizon: int
    region: str
    price: float
    trade_currency: str

@app.post("/predict/{model}", response_model=Dict[str, Any])
async def forecast_endpoint(model: str, request: PredictRequest):
    """Endpoint that cleans and prepares the data for the forecaster."""
    try:
        # Fetch data from MongoDB
        batch_scans = db.batch_scans.find({"client_id": request.customer_code})
        inventory = db.inventory.find({"batch_code": {"$exists": True}})
        data = pd.DataFrame(list(batch_scans) + list(inventory))

        # Clean data
        data.fillna(method='ffill', inplace=True)
        data.drop_duplicates(subset=['batch_code'], inplace=True)
        data = data[data['Region2'] == request.region]
        data['InvoiceDate'] = pd.to_datetime(data['InvoiceDate'])

        # Calculate days_between_invoices for average_use
        data = data.sort_values('InvoiceDate')
        data['days_between_invoices'] = data.groupby('client_id')['InvoiceDate'].diff().dt.days.fillna(1)
        data['average_use'] = data['Volume'] / data['days_between_invoices']

        # Normalize numerical fields
        scaler = MinMaxScaler()
        numerical_cols = ['Volume', 'NetSalesValue']
        if not data[numerical_cols].empty:
            data[numerical_cols] = scaler.fit_transform(data[numerical_cols])

        # Calculate time-series features
        time_series_features = get_client_item_time_series_features(data, request.client_name, request.customer_code)

        # Prepare parameters with ClientItemAdaptor
        adaptor = ClientItemAdaptor()
        params = {
            'item': data['SKU'].iloc[0] if not data.empty else 'unknown',
            'date': datetime.now(),
            'cust_name': request.client_name,
            'cust_code': request.customer_code,
            'cust_id': request.customer_code,
            'price': request.price,
            'region': request.region,
            'area': data['Area'].iloc[0] if not data.empty else 'unknown',
            'color': data['Colour'].iloc[0] if not data.empty else 'unknown',
            'container': data['Container'].iloc[0] if not data.empty else 'unknown',
            'currency': request.trade_currency,
            **time_series_features
        }
        adaptor.transform(params)
        prepared_data = adaptor.parameters()

        # Load model and predict (using DirectQuantileForecaster)
        forecaster = DirectQuantileForecaster(model, quantiles=[10, 50, 70, 90], horizon=request.predict_horizon)
        model_path = settings.model_dir / f"{model}.joblib"  # must still adjust path
        forecaster.load(model_path)
        result = forecaster.predict(adaptor)

        # Store result in MongoDB
        db.forecast_cache.insert_one({
            "client_id": request.customer_code,
            "batch_code": data['SKU'].iloc[0] if not data.empty else 'unknown',
            "cached_result": result,
            "period_to_predict": datetime.now().isoformat()
        })

        return result
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.get("/models", response_model=Dict[str, List[str]])
async def models_endpoint():
    """Endpoint that gets the list."""
    model_dir = settings.model_dir
    models = [f.stem for f in model_dir.glob("*.joblib") if f.is_file()]
    return {"models": models}