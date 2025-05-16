namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;

public interface ICustomerRegionCache
{
    Task SetCustomerRegion(long customerId, long regionId, CancellationToken cancellationToken);
    Task<long?> FindCustomerRegion(long customerId, CancellationToken cancellationToken);
}