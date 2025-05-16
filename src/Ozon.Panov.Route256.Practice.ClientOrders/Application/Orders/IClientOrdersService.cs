using Ozon.Panov.Route256.Practice.ClientOrders.Domain;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;

public interface IClientOrdersService
{
    Task CreateOrder(
        long customerId,
        IEnumerable<OrderItem> items,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OrderInfo>> GetCustomerOrders(
        long customerId,
        int limit,
        int offset,
        CancellationToken cancellationToken);
}