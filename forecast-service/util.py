import pandas as pd

def clean_data(df: pd.DataFrame) -> pd.DataFrame:
    """Cleaning function."""
    df = df.fillna(method='ffill')
    df = df.drop_duplicates(subset=['SKU'])
    return df

def calculate_average_use(df: pd.DataFrame) -> pd.DataFrame:
    """Calculate average_use = Volume / days_between_invoices."""
    df = df.sort_values('InvoiceDate')
    df['days_between_invoices'] = df.groupby('client_id')['InvoiceDate'].diff().dt.days.fillna(1)
    df['average_use'] = df['Volume'] / df['days_between_invoices']
    return df