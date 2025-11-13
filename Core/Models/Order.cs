using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Core.Models
{
    public class Order
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }

        [BsonElement("userId")]
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId UserId { get; set; }

        [BsonElement("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        [BsonElement("items")]
        public List<OrderItem> Items { get; set; } = new();

        [BsonElement("subtotal")]
        public decimal Subtotal { get; set; }

        [BsonElement("deliveryFee")]
        public decimal DeliveryFee { get; set; }

        [BsonElement("taxAmount")]
        public decimal TaxAmount { get; set; }

        [BsonElement("total")]
        public decimal Total { get; set; }

        [BsonElement("status")]
        public string Status { get; set; } = "Pending";

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class OrderItem
    {
        [BsonElement("productName")]
        public string ProductName { get; set; } = string.Empty;

        [BsonElement("sku")]
        public int Sku { get; set; }

        [BsonElement("quantity")]
        public int Quantity { get; set; }

        [BsonElement("pricePerUnit")]
        public decimal PricePerUnit { get; set; }

        [BsonElement("totalPrice")]
        public decimal TotalPrice { get; set; }
    }
}
