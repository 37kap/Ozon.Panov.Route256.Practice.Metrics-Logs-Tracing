namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders.OrdersProviding;

internal sealed class OrderNotFoundInProviderException(long orderId) :
    Exception($"Order {orderId} does not exist in provider");