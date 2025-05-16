using Ozon.Panov.Route256.Practice.ClientOrders.Domain;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;

public sealed record CreateOrderCommand(
    long CustomerId,
    long RegionId,
    string Comment,
    IEnumerable<OrderItem> Items);