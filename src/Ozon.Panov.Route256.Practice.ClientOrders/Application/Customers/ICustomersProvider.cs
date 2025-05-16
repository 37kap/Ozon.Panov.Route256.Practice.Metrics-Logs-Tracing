namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;

public interface ICustomersProvider
{
    Task<long> GetCustomerRegion(
        long customerId,
        CancellationToken cancellationToken);
}