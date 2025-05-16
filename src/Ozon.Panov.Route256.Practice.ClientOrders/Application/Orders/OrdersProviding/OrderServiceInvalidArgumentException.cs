namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders.OrdersProviding;

internal sealed class OrderServiceInvalidArgumentException(string message, Exception? innerException = null)
    : Exception(message, innerException);