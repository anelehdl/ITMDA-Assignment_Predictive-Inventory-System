using Core.Interfaces;
using Core.Models;
using Core.Models.DTO;
using Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.Services
{
    /// <summary>
    /// Service to communicate with ML prediction microservices
    /// </summary>
    public class PredictionService : IPredictionService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PredictionService> _logger;
        private readonly MongoDBContext _context;

        // Service URLs from configuration
        private readonly string _dataServiceUrl;
        private readonly string _modelServiceUrl;
        private readonly Dictionary<int, string> _predictServiceUrls;

        public PredictionService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<PredictionService> logger,
            MongoDBContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
            _context = context;

            // Initialize service URLs
            _dataServiceUrl = _configuration["PredictServices:DataService"] ?? "http://localhost:8520";
            _modelServiceUrl = _configuration["PredictServices:ModelService"] ?? "http://localhost:8620";

            _predictServiceUrls = new Dictionary<int, string>
            {
                { 1, _configuration["PredictServices:PredictH1"] ?? "http://localhost:8421" },
                { 5, _configuration["PredictServices:PredictH5"] ?? "http://localhost:8425" },
                { 10, _configuration["PredictServices:PredictH10"] ?? "http://localhost:8430" },
                { 20, _configuration["PredictServices:PredictH20"] ?? "http://localhost:8440" }
            };
        }

        /// <summary>
        /// Get prediction for a specific product and horizon
        /// </summary>
        public async Task<PredictionResponseDto> GetPredictionAsync(PredictionRequestDto request)
        {
            try
            {
                // Validate horizon
                if (!_predictServiceUrls.ContainsKey(request.Horizon))
                {
                    return new PredictionResponseDto
                    {
                        Success = false,
                        Message = $"Invalid horizon. Supported horizons: 1, 5, 10, 20"
                    };
                }

                var serviceUrl = _predictServiceUrls[request.Horizon];
                _logger.LogInformation($"Calling prediction service at {serviceUrl} for product {request.ProductCode}");

                //fetch inventory data 
                Inventory? inventoryItem = null;
                try
                {
                    int.TryParse(request.ProductCode, out int skuCode);
                    inventoryItem = await _context.InventoryCollection
                        .Find(inventoryItem => inventoryItem.Sku == skuCode)
                        .FirstOrDefaultAsync();
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Error fetching inventory data");
                }

                var requestBody = new
                {
                    item = request.ProductCode,  // Keep as string, don't parse to int
                    client_name = $"{inventoryItem?.UserCode ?? "L001"} - SA",  // Format: "L001 - SA"
                    customer_code = inventoryItem?.UserCode ?? "L001",
                    region = "SOUTH AFRICA",
                    area = "Western Cape",
                    price = 3600.0,
                    currency = "ZAR"
                };

                _logger.LogInformation($"Request body: {JsonSerializer.Serialize(requestBody)}");

                var response = await _httpClient.PostAsJsonAsync(
                    $"{serviceUrl}/predict",
                    requestBody
                );

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"Response status: {response.StatusCode}");
                _logger.LogInformation($"Response content: {responseContent}");

                if (response.IsSuccessStatusCode)
                {
                    var predictionData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseContent);

                    var cleanedData = new Dictionary<string, object>();
                    if (predictionData != null)
                    {
                        foreach (var kvp in predictionData)
                        {
                            if (kvp.Value.ValueKind == JsonValueKind.Number)
                            {
                                var value = kvp.Value.GetDecimal();
                                cleanedData[kvp.Key] = Math.Abs(value); // Remove negative sign
                            }
                            else
                            {
                                cleanedData[kvp.Key] = kvp.Value.ToString();
                            }
                        }
                    }

                    // Use q50 (median) as the primary prediction
                    decimal? predictedDemand = cleanedData.ContainsKey("q50")
                        ? (decimal)cleanedData["q50"]
                        : null;

                    return new PredictionResponseDto
                    {
                        Success = true,
                        Message = "Prediction successful",
                        PredictedDemand = predictedDemand,
                        Horizon = request.Horizon,
                        ProductCode = request.ProductCode,
                        Confidence = 85m, // You can calculate this based on the quantile spread
                        Timestamp = DateTime.UtcNow,
                        AdditionalData = predictionData?.ToDictionary(
                            kvp => kvp.Key,
                            kvp => (object)kvp.Value.ToString()
                        )
                    };
                }
                else
                {
                    _logger.LogError($"prediction service error: {response.StatusCode} - {responseContent}");

                    return new PredictionResponseDto
                    {
                        Success = false,
                        Message = $"Prediction service returned {response.StatusCode}",
                        Horizon = request.Horizon,
                        ProductCode = request.ProductCode
                    };
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling prediction service");
                return new PredictionResponseDto
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    Horizon = request.Horizon,
                    ProductCode = request.ProductCode
                };
            }

        }

        /// <summary>
        /// Get predictions for all horizons (1, 5, 10, 20 days)
        /// </summary>
        public async Task<List<PredictionResponseDto>> GetMultiHorizonPredictionsAsync(string productCode)
        {
            var predictions = new List<PredictionResponseDto>();
            var horizons = new[] { 1, 5, 10, 20 };

            // Call all prediction services in parallel
            var tasks = horizons.Select(horizon =>
                GetPredictionAsync(new PredictionRequestDto
                {
                    ProductCode = productCode,
                    Horizon = horizon
                })
            );

            predictions.AddRange(await Task.WhenAll(tasks));
            return predictions;
        }

        /// <summary>
        /// Check health status of all prediction services
        /// </summary>
        public async Task<Dictionary<string, bool>> CheckServicesHealthAsync()
        {
            var healthStatus = new Dictionary<string, bool>();

            // Check data service
            healthStatus["data-service"] = await CheckServiceHealthAsync(_dataServiceUrl);

            // Check model service
            healthStatus["model-service"] = await CheckServiceHealthAsync(_modelServiceUrl);

            // Check all predict services
            foreach (var kvp in _predictServiceUrls)
            {
                healthStatus[$"predict-h{kvp.Key}"] = await CheckServiceHealthAsync(kvp.Value);
            }

            return healthStatus;
        }

        /// <summary>
        /// Get available products from data service
        /// </summary>
        public async Task<List<ProductDataDto>> GetAvailableProductsAsync()
        {
            try
            {
                _logger.LogInformation($"Fetching products from data service: {_dataServiceUrl}");

                var response = await _httpClient.GetAsync($"{_dataServiceUrl}/products");

                if (response.IsSuccessStatusCode)
                {
                    var products = await response.Content.ReadFromJsonAsync<List<ProductDataDto>>();
                    return products ?? new List<ProductDataDto>();
                }
                else
                {
                    _logger.LogError($"Data service returned {response.StatusCode}");
                    return new List<ProductDataDto>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products from data service");
                return new List<ProductDataDto>();
            }
        }

        /// <summary>
        /// Helper method to check if a service is healthy
        /// </summary>
        private async Task<bool> CheckServiceHealthAsync(string serviceUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{serviceUrl}/health",
                    new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}