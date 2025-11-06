namespace Core.Models.DTO
{
    /// <summary>
    /// Request DTO for getting predictions from ML services
    /// </summary>
    public class PredictionRequestDto
    {
        /// <summary>
        /// Product code or identifier to predict for
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// Prediction horizon (1, 5, 10, or 20 days)
        /// </summary>
        public int Horizon { get; set; } = 1;

        /// <summary>
        /// Optional: Additional parameters for the prediction model
        /// </summary>
        public Dictionary<string, object>? AdditionalParameters { get; set; }

    }

    /// <summary>
    /// Response DTO containing prediction results
    /// </summary>
    public class PredictionResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }

        /// <summary>
        /// Predicted demand value
        /// </summary>
        public decimal? PredictedDemand { get; set; }

        /// <summary>
        /// Prediction horizon used
        /// </summary>
        public int Horizon { get; set; }

        /// <summary>
        /// Product code that was predicted
        /// </summary>
        public string? ProductCode { get; set; }

        /// <summary>
        /// Confidence level of the prediction (0-100)
        /// </summary>
        public decimal? Confidence { get; set; }

        /// <summary>
        /// Timestamp of when prediction was made
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional data from the prediction service
        /// </summary>
        public Dictionary<string, object>? AdditionalData { get; set; }
    }

    /// <summary>
    /// DTO for getting available products from data service
    /// </summary>
    public class ProductDataDto
    {
        public string? ProductCode { get; set; }
        public string? ProductName { get; set; }
        public decimal? CurrentStock { get; set; }
        public List<HistoricalDataPoint>? HistoricalData { get; set; }
    }

    public class HistoricalDataPoint
    {
        public DateTime Date { get; set; }
        public decimal Demand { get; set; }
    }
}