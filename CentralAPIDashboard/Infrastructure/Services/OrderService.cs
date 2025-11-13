using Core.Interfaces;
using Core.Models;
using Core.Models.DTO;
using Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IMongoDBContext _context;

        public OrderService(IMongoDBContext context)
        {
            _context = context;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(string userId, CreateOrderDto orderDto)
        {
            Console.WriteLine($"=== Creating Order for User: {userId} ===");

            if (!ObjectId.TryParse(userId, out var userObjectId))
            {
                throw new ArgumentException("Invalid user ID format");
            }

            var orderNumber = GenerateOrderNumber();
            Console.WriteLine($"Generated Order Number: {orderNumber}");

            var order = new Order
            {
                UserId = userObjectId,
                OrderNumber = orderNumber,
                Items = orderDto.Items.Select(item => new OrderItem
                {
                    ProductName = item.ProductName,
                    Sku = item.Sku,
                    Quantity = item.Quantity,
                    PricePerUnit = item.PricePerUnit,
                    TotalPrice = item.TotalPrice
                }).ToList(),
                Subtotal = orderDto.Subtotal,
                DeliveryFee = orderDto.DeliveryFee,
                TaxAmount = orderDto.TaxAmount,
                Total = orderDto.Total,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            Console.WriteLine($"About to insert order into MongoDB...");
            await _context.OrdersCollection.InsertOneAsync(order);
            Console.WriteLine($"Order inserted! Order ID: {order.Id}");

            return MapToResponseDto(order);
        }

        public async Task<List<OrderResponseDto>> GetUserOrdersAsync(string userId)
        {
            if (!ObjectId.TryParse(userId, out var userObjectId))
            {
                throw new ArgumentException("Invalid user ID format");
            }

            var orders = await _context.OrdersCollection
                .Find(o => o.UserId == userObjectId)
                .SortByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(MapToResponseDto).ToList();
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(string orderId, string userId)
        {
            if (!ObjectId.TryParse(orderId, out var orderObjectId) ||
                !ObjectId.TryParse(userId, out var userObjectId))
            {
                throw new ArgumentException("Invalid ID format");
            }

            var order = await _context.OrdersCollection
                .Find(o => o.Id == orderObjectId && o.UserId == userObjectId)
                .FirstOrDefaultAsync();

            return order != null ? MapToResponseDto(order) : null;
        }

        private string GenerateOrderNumber()
        {
            // Generate order number
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            var randomPart = new Random().Next(10000, 99999);
            return $"ORD-{datePart}-{randomPart}";
        }

        private OrderResponseDto MapToResponseDto(Order order)
        {
            return new OrderResponseDto
            {
                Id = order.Id.ToString(),
                OrderNumber = order.OrderNumber,
                Items = order.Items.Select(item => new OrderItemDto
                {
                    ProductName = item.ProductName,
                    Sku = item.Sku,
                    Quantity = item.Quantity,
                    PricePerUnit = item.PricePerUnit,
                    TotalPrice = item.TotalPrice
                }).ToList(),
                Subtotal = order.Subtotal,
                DeliveryFee = order.DeliveryFee,
                TaxAmount = order.TaxAmount,
                Total = order.Total,
                Status = order.Status,
                CreatedAt = order.CreatedAt
            };
        }
    }
}
