using Ozon.Panov.Route256.Practice.ClientOrders.Domain;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders.OrdersProviding;

public sealed record OrdersResult(
    IReadOnlyCollection<CustomerOrder> Orders,
    long TotalCount,
    long FilteredCount);