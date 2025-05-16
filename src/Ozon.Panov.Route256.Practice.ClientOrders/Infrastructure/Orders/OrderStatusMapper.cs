using Ozon.Panov.Route256.Practice.ClientOrders.Domain;
using ClientOrderStatusGrpc = Ozon.Panov.Route256.Practice.ClientOrders.Proto.ClientOrdersGrpc.OrderStatus;
using OrderStatusGrpc = Ozon.Route256.OrderService.Proto.OrderGrpc.OrderStatus;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Orders;

internal static class OrderStatusMapper
{
    public static OrderStatus FromGrpcStatus(this OrderStatusGrpc grpcStatus)
    {
        OrderStatus status = grpcStatus switch
        {
            OrderStatusGrpc.Undefined => OrderStatus.Undefined,
            OrderStatusGrpc.New => OrderStatus.New,
            OrderStatusGrpc.Canceled => OrderStatus.Canceled,
            OrderStatusGrpc.Delivered => OrderStatus.Delivered,
            _ => throw new ArgumentOutOfRangeException(nameof(grpcStatus), grpcStatus, null)
        };

        return status;
    }

    public static ClientOrderStatusGrpc ToGrpcStatus(this OrderStatus status)
    {
        ClientOrderStatusGrpc grpcStatus = status switch
        {
            OrderStatus.Undefined => ClientOrderStatusGrpc.Undefined,
            OrderStatus.New => ClientOrderStatusGrpc.New,
            OrderStatus.Canceled => ClientOrderStatusGrpc.Canceled,
            OrderStatus.Delivered => ClientOrderStatusGrpc.Delivered,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
        };

        return grpcStatus;
    }

}