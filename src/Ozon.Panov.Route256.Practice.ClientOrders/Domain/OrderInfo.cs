namespace Ozon.Panov.Route256.Practice.ClientOrders.Domain;

public sealed record OrderInfo(
    long OrderId,
    OrderStatus Status,
    DateTime OrderDate);