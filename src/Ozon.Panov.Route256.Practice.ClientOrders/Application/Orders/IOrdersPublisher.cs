using Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;

public interface IOrdersPublisher
{
    Task Publish(
        CreateOrderCommand order,
        CancellationToken token);
}