using Core.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Dashboard.Controllers
{
    [Authorize(Roles = "admin,staff")]
    public class PredictionController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(
            IHttpClientFactory httpClientFactory,
            ILogger<PredictionController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Display the main forecasts page
        /// </summary>
        public IActionResult Index()
        {
            return View("~/Views/Prediction/Index.cshtml");
        }

        /// <summary>
        /// Get prediction for a specific product and horizon
        /// API endpoint: GET /api/prediction/predict/{productCode}/{horizon}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetPrediction(string productCode, int horizon)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CentralAPIDashboard");
                var token = User.Claims.FirstOrDefault(c => c.Type == "Token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync($"/api/prediction/predict/{productCode}/{horizon}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var prediction = JsonSerializer.Deserialize<PredictionResponseDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Json(prediction);
                }

                _logger.LogWarning("Failed to get prediction for {ProductCode} with horizon {Horizon}. Status: {Status}",
                    productCode, horizon, response.StatusCode);

                return Json(new PredictionResponseDto
                {
                    Success = false,
                    Message = $"Failed to get prediction: {response.StatusCode}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting prediction for {ProductCode}", productCode);
                return Json(new PredictionResponseDto
                {
                    Success = false,
                    Message = "An error occurred while fetching prediction"
                });
            }
        }

        /// <summary>
        /// Get predictions for all horizons (1, 5, 10, 20 days)
        /// API endpoint: GET /api/prediction/predict/multi/{productCode}
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMultiHorizonPredictions(string productCode)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CentralAPIDashboard");
                var token = User.Claims.FirstOrDefault(c => c.Type == "Token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync($"/api/prediction/predict/multi/{productCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var predictions = JsonSerializer.Deserialize<List<PredictionResponseDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Json(predictions);
                }

                _logger.LogWarning("Failed to get multi-horizon predictions for {ProductCode}. Status: {Status}",
                    productCode, response.StatusCode);

                return Json(new List<PredictionResponseDto>
                {
                    new PredictionResponseDto
                    {
                        Success = false,
                        Message = $"Failed to get predictions: {response.StatusCode}"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting multi-horizon predictions for {ProductCode}", productCode);
                return Json(new List<PredictionResponseDto>
                {
                    new PredictionResponseDto
                    {
                        Success = false,
                        Message = "An error occurred while fetching predictions"
                    }
                });
            }
        }

        /// <summary>
        /// Get available products from the data service
        /// API endpoint: GET /api/prediction/products
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CentralAPIDashboard");
                var token = User.Claims.FirstOrDefault(c => c.Type == "Token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                var response = await client.GetAsync("/api/prediction/products");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var products = JsonSerializer.Deserialize<List<ProductDataDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Json(products);
                }

                _logger.LogWarning("Failed to get products. Status: {Status}", response.StatusCode);
                return Json(new List<ProductDataDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return Json(new List<ProductDataDto>());
            }
        }

        /// <summary>
        /// Check health status of prediction services
        /// API endpoint: GET /api/prediction/health
        /// </summary>
        [HttpGet]
        [AllowAnonymous] // Allow health checks without full authentication
        public async Task<IActionResult> CheckHealth()
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CentralAPIDashboard");

                var response = await client.GetAsync("/api/prediction/health");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var healthStatus = JsonSerializer.Deserialize<Dictionary<string, object>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Json(healthStatus);
                }

                return Json(new { Status = "Unhealthy", Message = response.StatusCode.ToString() });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking prediction service health");
                return Json(new { Status = "Error", Message = ex.Message });
            }
        }

        /// <summary>
        /// Detailed view for a specific product's predictions
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ProductDetails(string productCode)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("CentralAPIDashboard");
                var token = User.Claims.FirstOrDefault(c => c.Type == "Token")?.Value;

                if (!string.IsNullOrEmpty(token))
                {
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                // Get all horizon predictions for the product
                var response = await client.GetAsync($"/api/prediction/predict/multi/{productCode}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var predictions = JsonSerializer.Deserialize<List<PredictionResponseDto>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    ViewBag.ProductCode = productCode;
                    return View(predictions);
                }

                ViewBag.Error = "Failed to load product predictions";
                ViewBag.ProductCode = productCode;
                return View(new List<PredictionResponseDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for {ProductCode}", productCode);
                ViewBag.Error = "An error occurred while loading product predictions";
                ViewBag.ProductCode = productCode;
                return View(new List<PredictionResponseDto>());
            }
        }
    }
}