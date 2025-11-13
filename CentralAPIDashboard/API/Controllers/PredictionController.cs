using Core.Interfaces;
using Core.Models;
using Core.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace API.Controllers
{
    /// <summary>
    /// Controller for ML prediction operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "admin,staff")] 
    public class PredictionController : ControllerBase
    {
        private readonly IPredictionService _predictionService;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(
            IPredictionService predictionService,
            ILogger<PredictionController> logger)
        {
            _predictionService = predictionService;
            _logger = logger;
        }

        [HttpGet("predict/{productCode}/{horizon}")]
        [ProducesResponseType(typeof(PredictionResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetPredictionByHorizon(string productCode, int horizon)
        {
            _logger.LogInformation($"Prediction request for product: {productCode}, horizon: {horizon}");

            if (string.IsNullOrEmpty(productCode))
            {
                return BadRequest(new { Message = "Product code is required" });
            }
            if (!new[] {1,5,10,20}.Contains(horizon))
            {
                return BadRequest(new { Message = "Horizon must be 1, 5, 10, or 20 days" });
            }

            var result = await _predictionService.GetPredictionAsync(new PredictionRequestDto
            {
                ProductCode = productCode,
                Horizon = horizon
            });

            if (result.Success)
                return Ok(result);

            return BadRequest(result);
        }

        /// <summary>
        /// Get predictions for all horizons (1, 5, 10, 20 days) for a product
        /// </summary>
        /// <param name="productCode">Product code to predict for</param>
        /// <returns>List of predictions for all horizons</returns>
        [HttpGet("predict/multi/{productCode}")]
        [ProducesResponseType(typeof(List<PredictionResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMultiHorizonPredictions(string productCode)
        {
            _logger.LogInformation($"Multi-horizon prediction request for product: {productCode}");

            if (string.IsNullOrEmpty(productCode))
            {
                return BadRequest(new { Message = "Product code is required" });
            }

            //call service
            var results = await _predictionService.GetMultiHorizonPredictionsAsync(productCode);
            return Ok(results);
        }

        /// <summary>
        /// Check health status of all prediction services
        /// </summary>
        /// <returns>Dictionary of service names and their health status</returns>
        [HttpGet("health")]
        [AllowAnonymous] // Allow health checks without authentication
        [ProducesResponseType(typeof(Dictionary<string, bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckServicesHealth()
        {
            _logger.LogInformation("Checking prediction services health");

            var healthStatus = await _predictionService.CheckServicesHealthAsync();

            // Return 503 if any critical service is down
            var allHealthy = healthStatus.Values.All(status => status);

            if (allHealthy)
                return Ok(new { Status = "Healthy", Services = healthStatus });

            return StatusCode(503, new { Status = "Unhealthy", Services = healthStatus });
        }


        /// <summary>
        /// Get available products from MongoDB ForecastDB Inventory collection
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                _logger.LogInformation("Fetching available products from inventory");

                var inventoryService = HttpContext.RequestServices.GetService<IInventoryService>();

                if (inventoryService == null)
                {
                    _logger.LogWarning("InventoryService not available");
                    return Ok(new List<ProductDataDto>());
                }

                var distinctItemCodes = await inventoryService.GetDistinctItemCodesAsync();

                //convert to ProductDataDto format
                var products = distinctItemCodes.Select(sku => new ProductDataDto
                {
                    ProductCode = sku,
                    ProductName = $"SKU {sku}",
                    CurrentStock = null
                }).ToList();

                _logger.LogInformation("Found {Count} distinct products in inventory", products.Count);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products from inventory");
                return Ok(new List<ProductDataDto>());
            }
        }

    }
}
