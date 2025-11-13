using Core.Models;
using MongoDB.Driver;

namespace Infrastructure.Data
{
    public interface IMongoDBContext        //need this interface for tests, as test cannot directly mock concrete class MongoDBContext
    {
        //UserDB Collections
        IMongoCollection<Staff> StaffCollection { get; }
        IMongoCollection<Role> RolesCollection { get; }
        IMongoCollection<Authentication> AuthenticationCollection { get; }
        IMongoCollection<Client> ClientCollection { get; }
        IMongoCollection<Order> OrdersCollection { get; }

        //ForecastDB Collections
        IMongoCollection<Inventory> InventoryCollection { get; }
        
    }
}
