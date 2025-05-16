using Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;
using Ozon.Panov.Route256.Practice.ClientOrders.Domain;
using System.Diagnostics;
using System.Transactions;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;

public sealed class ClientOrdersService(
    ICustomersProvider customersProvider,
    IOutboxService outboxService,
    IClientOrderRepository clientOrderRepository,
    IOrderMetrics metrics) :
    IClientOrdersService
{
    public async Task CreateOrder(
        long customerId,
        IEnumerable<OrderItem> items,
        CancellationToken cancellationToken)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        try
        {
            var regionId = await customersProvider.GetCustomerRegion(
                customerId, cancellationToken);

            var identifier = Guid.NewGuid().ToString("D");

            var message = new CreateOrderCommand(
                CustomerId: customerId,
                RegionId: regionId,
                Comment: identifier,
                Items: items);

            var clientOrder = new CustomerOrder
            {
                OrderId = 0,
                Comment = identifier,
                CustomerId = customerId,
                Status = OrderStatus.New,
                CreatedAt = DateTime.UtcNow
            };

            using var ts = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            await outboxService.CreateOutboxMessage(customerId, message, cancellationToken);
            await clientOrderRepository.Insert(clientOrder, cancellationToken);

            ts.Complete();
        }
        finally
        {
            stopwatch.Stop();
            metrics.RecordOrderCreationTime(stopwatch.Elapsed.TotalSeconds, customerId);
        }
    }

    public async Task<IReadOnlyList<OrderInfo>> GetCustomerOrders(
        long customerId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        var orders = await clientOrderRepository.GetByCustomerId(
            customerId, limit, offset, cancellationToken);

        return [.. orders.Select(order => new OrderInfo(
            OrderId: order.OrderId,
            Status: order.Status,
            OrderDate: order.CreatedAt))];
    }
}