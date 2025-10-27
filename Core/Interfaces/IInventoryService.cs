using Core.Models.DTO;

namespace Core.Interfaces
{
    public interface IInventoryService
    {
        //getting errors here
        Task<StockMetricsOverviewDto> GetStockMetricsOverviewAsync();
        Task<ClientInventoryStatsDto?> GetClientInventoryStatsAsync(string clientId);
        Task<List<InventoryDto>> GetInventoryByFilterAsync(InventoryFilterDto filter);
    }
}
