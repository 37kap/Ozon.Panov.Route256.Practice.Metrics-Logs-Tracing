namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;

public interface IOutboxService
{
    Task CreateOutboxMessage(
        long customerId,
        CreateOrderCommand command,
        CancellationToken cancellationToken);
}