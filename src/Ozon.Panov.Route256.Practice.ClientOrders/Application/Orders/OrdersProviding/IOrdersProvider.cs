namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders.OrdersProviding;

public interface IOrdersProvider
{
    Task<OrdersResult> GetOrders(
        OrdersQuery query,
        CancellationToken cancellationToken);
}
