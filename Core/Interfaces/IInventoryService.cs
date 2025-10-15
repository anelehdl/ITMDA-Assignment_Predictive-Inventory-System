using Core.Models.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
