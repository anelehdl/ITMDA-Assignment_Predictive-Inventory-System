using Core.Interfaces;
using Core.Models;
using Core.Models.DTO;
using Infrastructure.Data;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Services
{

    /// <summary>
    /// InventoryService manages stock metrics and inventory data.
    /// Provides business intelligence and analytics for client inventory.
    /// Key features:
    /// - aggregates inventory data across clients
    /// - calculates usage statistics and metrics
    /// - generates overview dashboard and client sepcific reports
    /// - provides filtering
    /// </summary>

    public class InventoryService : IInventoryService
    {
        private readonly MongoDBContext _context;

        public InventoryService(MongoDBContext context)
        {
            _context = context;
        }


        /// <summary>
        ///Generates an overview of stock metrics across all clients;
        ///calculates aggregated statistics and top clients.
        /// </summary>

        public async Task<StockMetricsOverviewDto> GetStockMetricsOverviewAsync()
        {
            // ============================================================
            // STEP 1: Retrieve All Data
            // ============================================================
            //get all inventory records from ForecastDB
            var allInventory = await _context.InventoryCollection
                .Find(_ => true)
                .ToListAsync();

            //get all clients from UserDB
            var allClients = await _context.ClientCollection
                .Find(_ => true)
                .ToListAsync();

            var clientStats = new Dictionary<string, ClientInventoryStatsDto>();

            // ============================================================
            // STEP 2: Group Inventory by Client
            // ============================================================
            //group all inventory records by userId
            var inventoryByClient = allInventory.GroupBy(i => i.UserId.ToString());

            // ============================================================
            // STEP 3: Calculate Stats for Each Client
            // ============================================================
            foreach (var group in inventoryByClient)
            {
                var clientId = group.Key;

                //find corresponding client record
                var client = allClients.FirstOrDefault(c => c.Id.ToString() == clientId);
                if (client == null) continue;

                //sort orders by date (most recent)
                var orders = group.OrderByDescending(i => i.OrderDate).ToList();
                var lastOrder = orders.FirstOrDefault();

                //calculate average daily usage, ignoring nulls
                var validDailyUsages = orders.Where(o => o.AverageDailyUse.HasValue)
                                            .Select(o => o.AverageDailyUse.Value);

                //build client stats DTO
                var stats = new ClientInventoryStatsDto
                {
                    ClientId = clientId,
                    UserCode = client.UserCode,
                    Username = client.Username,
                    TotalOrders = orders.Count,
                    TotalLitres = orders.Sum(o => o.Litres),
                    AverageDailyUsage = validDailyUsages.Any() ? validDailyUsages.Average() : 0,
                    LastOrderDate = lastOrder?.OrderDate,

                    //get 5 most recent orders
                    RecentOrders = orders.Take(5).Select(o => new InventoryDto
                    {
                        Id = o.Id.ToString(),
                        Sku = o.Sku,
                        SkuDescription = o.SkuDescription,
                        UserCode = o.UserCode,
                        OrderDate = o.OrderDate,
                        PreviousOrderDate = o.PreviousOrderDate,
                        Litres = o.Litres,
                        DaysBetweenOrders = o.DaysBetweenOrders,
                        AverageDailyUse = o.AverageDailyUse ?? 0, // Handle null
                        UserId = o.UserId.ToString()
                    }).ToList(),

                    //calculate SKU breakdown
                    SkuBreakdown = orders.GroupBy(o => o.SkuDescription)
                        .ToDictionary(g => g.Key, g => g.Sum(o => o.Litres))
                };

                clientStats[clientId] = stats;
            }

            // ============================================================
            // STEP 4: Aggregate Overall Statistics
            // ============================================================
            //filter out null values for overall average daily usage
            var validOverallUsages = allInventory.Where(i => i.AverageDailyUse.HasValue)
                                                 .Select(i => i.AverageDailyUse.Value);

            //build overview DTO with aggregated data
            var overview = new StockMetricsOverviewDto
            {
                TotalClients = clientStats.Count,
                TotalOrders = allInventory.Count,
                TotalLitres = allInventory.Sum(i => i.Litres),
                AverageDailyUsageAllClients = validOverallUsages.Any() ? validOverallUsages.Average() : 0,
                
                //get top 10 clients by total litres ordered
                TopClients = clientStats.Values
                    .OrderByDescending(c => c.TotalLitres)
                    .Take(10)
                    .ToList(),

                //calculate overall SKU distribution
                SkuDistribution = allInventory
                    .GroupBy(i => i.SkuDescription)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Litres))
            };

            return overview;
        }


        /// <summary>
        ///Retrieves detailed inventory stats for a specific client by ID;
        ///includes order history, usage patterns, and SKU breakdown.
        /// </summary>

        public async Task<ClientInventoryStatsDto?> GetClientInventoryStatsAsync(string clientId)
        {
            //convert string ID to ObjectId
            if (!ObjectId.TryParse(clientId, out var objectId))
                return null;

            // ============================================================
            // STEP 1: Find CLient Record
            // ============================================================
            var client = await _context.ClientCollection
                .Find(c => c.Id == objectId)
                .FirstOrDefaultAsync();

            if (client == null)
                return null;

            // ============================================================
            // STEP 2: Get Client Inventory Records
            // ============================================================
            //query inventory by userId
            var inventory = await _context.InventoryCollection
                .Find(i => i.UserId == objectId)
                .SortByDescending(i => i.OrderDate)
                .ToListAsync();

            // ============================================================
            // STEP 3: Handle No Inventory Case
            // ============================================================
            if (!inventory.Any())
            {
                //return empty stats if client has no orders
                return new ClientInventoryStatsDto
                {
                    ClientId = clientId,
                    UserCode = client.UserCode,
                    Username = client.Username,
                    TotalOrders = 0,
                    TotalLitres = 0,
                    AverageDailyUsage = 0,
                    LastOrderDate = null
                };
            }

            // ============================================================
            // STEP 4: Calculate Client-Specific Stats
            // ============================================================
            var lastOrder = inventory.First(); //most recent order

            //calculate average daily usage, ignoring nulls
            var validDailyUsages = inventory.Where(i => i.AverageDailyUse.HasValue)
                                           .Select(i => i.AverageDailyUse.Value);

            return new ClientInventoryStatsDto
            {
                ClientId = clientId,
                UserCode = client.UserCode,
                Username = client.Username,
                TotalOrders = inventory.Count,
                TotalLitres = inventory.Sum(i => i.Litres),
                AverageDailyUsage = validDailyUsages.Any() ? validDailyUsages.Average() : 0,
                LastOrderDate = lastOrder.OrderDate,

                //get 10 most recent orders
                RecentOrders = inventory.Take(10).Select(i => new InventoryDto
                {
                    Id = i.Id.ToString(),
                    Sku = i.Sku,
                    SkuDescription = i.SkuDescription,
                    UserCode = i.UserCode,
                    OrderDate = i.OrderDate,
                    PreviousOrderDate = i.PreviousOrderDate,
                    Litres = i.Litres,
                    DaysBetweenOrders = i.DaysBetweenOrders,
                    AverageDailyUse = i.AverageDailyUse ?? 0,
                    UserId = i.UserId.ToString()
                }).ToList(),

                //calculate SKU breakdown
                SkuBreakdown = inventory
                    .GroupBy(i => i.SkuDescription)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Litres))
            };
        }


        /// <summary>
        ///Retrieves inventory records,
        ///supports filtering by client ID, user code, date range, and SKU.
        /// </summary>

        public async Task<List<InventoryDto>> GetInventoryByFilterAsync(InventoryFilterDto filter)
        {
            // ============================================================
            // STEP 1: Build MongoDB Filter
            // ============================================================
            //create filter builder
            var filterBuilder = Builders<Inventory>.Filter;
            var filters = new List<FilterDefinition<Inventory>>();

            //add client ID filter if provided
            if (!string.IsNullOrEmpty(filter.ClientId) && ObjectId.TryParse(filter.ClientId, out var clientObjectId))
            {
                filters.Add(filterBuilder.Eq(i => i.UserId, clientObjectId));
            }

            //add user code filter if provided
            if (!string.IsNullOrEmpty(filter.UserCode))
            {
                filters.Add(filterBuilder.Eq(i => i.UserCode, filter.UserCode));
            }

            //add start date filter
            if (filter.StartDate.HasValue)
            {
                filters.Add(filterBuilder.Gte(i => i.OrderDate, filter.StartDate.Value));
            }

            //add end date filter
            if (filter.EndDate.HasValue)
            {
                filters.Add(filterBuilder.Lte(i => i.OrderDate, filter.EndDate.Value));
            }

            //add SKU filter if provided
            if (filter.Sku.HasValue)
            {
                filters.Add(filterBuilder.Eq(i => i.Sku, filter.Sku.Value));
            }

            // ============================================================
            // STEP 2: Combine FIlters and Query
            // ============================================================
            //combine all filters with AND logic
            var combinedFilter = filters.Any()
                ? filterBuilder.And(filters)
                : filterBuilder.Empty; //empty filter = get all records

            //execute query with combined filter
            var inventory = await _context.InventoryCollection
                .Find(combinedFilter)
                .SortByDescending(i => i.OrderDate) //most recent first
                .ToListAsync();

            // ============================================================
            // STEP 3: Convert to DTOs
            // ============================================================
            //map MongoDB models to DTOs for API response
            return inventory.Select(i => new InventoryDto
            {
                Id = i.Id.ToString(),
                Sku = i.Sku,
                SkuDescription = i.SkuDescription,
                UserCode = i.UserCode,
                OrderDate = i.OrderDate,
                PreviousOrderDate = i.PreviousOrderDate,
                Litres = i.Litres,
                DaysBetweenOrders = i.DaysBetweenOrders,
                AverageDailyUse = i.AverageDailyUse ?? 0, //handles nulls
                UserId = i.UserId.ToString()
            }).ToList();
        }
    }
}