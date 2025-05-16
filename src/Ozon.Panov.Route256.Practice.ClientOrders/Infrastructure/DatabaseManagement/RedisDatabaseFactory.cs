using StackExchange.Redis;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.DatabaseManagement;

internal sealed class RedisDatabaseFactory(IConnectionMultiplexer connectionMultiplexer)
{
    public IDatabase GetDatabase() => connectionMultiplexer.GetDatabase();
}