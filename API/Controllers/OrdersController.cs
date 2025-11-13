using Core.Interfaces;
using Core.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// POST /api/orders
        /// Create a new order for the authenticated user
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto orderDto)
        {
            try
            {
                // Get user ID from JWT token claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var order = await _orderService.CreateOrderAsync(userId, orderDto);

                return Ok(new
                {
                    success = true,
                    message = "Order created successfully",
                    order = order
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error creating order: {ex.Message}" });
            }
        }

        /// <summary>
        /// GET /api/orders
        /// Get all orders for the authenticated user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetUserOrders()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var orders = await _orderService.GetUserOrdersAsync(userId);

                return Ok(new
                {
                    success = true,
                    orders = orders
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving orders: {ex.Message}" });
            }
        }

        /// <summary>
        /// GET /api/orders/{id}
        /// Get a specific order by ID for the authenticated user
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var order = await _orderService.GetOrderByIdAsync(id, userId);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                return Ok(new
                {
                    success = true,
                    order = order
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving order: {ex.Message}" });
            }
        }
    }
}
