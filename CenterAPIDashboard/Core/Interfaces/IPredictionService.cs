using Core.Models.DTO;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for interacting with ML prediction microservices
    /// </summary>
    public interface IPredictionService
    {
        /// <summary>
        /// Get prediction for a specific product and horizon
        /// </summary>
        Task<PredictionResponseDto> GetPredictionAsync(PredictionRequestDto request);

        /// <summary>
        /// Get predictions for multiple horizons at once
        /// </summary>
        Task<List<PredictionResponseDto>> GetMultiHorizonPredictionsAsync(string productCode);

        /// <summary>
        /// Check health status of prediction services
        /// </summary>
        Task<Dictionary<string, bool>> CheckServicesHealthAsync();

        /// <summary>
        /// Get available products from data service
        /// </summary>
        Task<List<ProductDataDto>> GetAvailableProductsAsync();
    }
}
