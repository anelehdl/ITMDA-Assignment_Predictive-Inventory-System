namespace Core.Models.DTO
{
    public class OrderResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public List<OrderItemDto> Items { get; set; } = new();
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
