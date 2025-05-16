using Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Customers;

public sealed class CachedCustomersProvider(
    ICustomersProvider innerProvider, ICustomerRegionCache cache)
    : ICustomersProvider
{
    public async Task<long> GetCustomerRegion(long customerId, CancellationToken cancellationToken)
    {
        var cachedRegion = await cache.FindCustomerRegion(customerId, cancellationToken);
        if (cachedRegion.HasValue)
        {
            return cachedRegion.Value;
        }

        var regionId = await innerProvider.GetCustomerRegion(customerId, cancellationToken);
        await cache.SetCustomerRegion(customerId, regionId, cancellationToken);

        return regionId;
    }
}
