namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders.OrdersProviding;

public sealed record OrdersQuery(
    int Limit,
    int Offset,
    long? OrderId = null,
    long? CustomerId = null,
    string? Comment = null);