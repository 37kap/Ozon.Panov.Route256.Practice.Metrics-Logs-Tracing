using Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.DatabaseManagement;
using StackExchange.Redis;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Customers;

internal sealed class CustomerRegionRedisRepository : ICustomerRegionCache
{
    private readonly IDatabase _database;
    private readonly IOrderMetrics _metrics;

    public CustomerRegionRedisRepository(
        RedisDatabaseFactory redisDatabaseFactory,
        IOrderMetrics metrics)
    {
        _metrics = metrics;
        _database = redisDatabaseFactory.GetDatabase();
    }

    public async Task SetCustomerRegion(long customerId, long regionId, CancellationToken cancellationToken)
    {
        var key = GetKey(customerId);
        await _database.StringSetAsync(key, regionId).WaitAsync(cancellationToken);
        _metrics.IncrementCacheSize();
    }

    public async Task<long?> FindCustomerRegion(long customerId, CancellationToken cancellationToken)
    {
        var key = GetKey(customerId);
        var value = await _database.StringGetAsync(key).WaitAsync(cancellationToken);
        if (value.IsNull)
        {
            return null;
        }
        return (int)value;
    }

    private static string GetKey(long customerId)
    {
        return $"customer:{customerId}:region";
    }
}