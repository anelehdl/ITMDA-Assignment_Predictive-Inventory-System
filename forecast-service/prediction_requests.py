from pydantic import BaseModel


class ClientItemPRequest(BaseModel):
    item: str
    client_name: str
    customer_code: str
    region: str
    area: str
    price: float
    currency: str
