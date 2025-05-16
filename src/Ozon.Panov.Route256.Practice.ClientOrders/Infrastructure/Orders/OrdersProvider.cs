using Grpc.Core;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders.OrdersProviding;
using Ozon.Panov.Route256.Practice.ClientOrders.Domain;
using Ozon.Route256.OrderService.Proto.OrderGrpc;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Orders;

internal sealed class OrdersProvider(OrderGrpc.OrderGrpcClient orderGrpcClient) : IOrdersProvider
{
    public async Task<OrdersResult> GetOrders(
        OrdersQuery query,
        CancellationToken cancellationToken)
    {
        var request = new V1QueryOrdersRequest
        {
            Limit = query.Limit,
            Offset = query.Offset
        };

        if (query.OrderId is { } orderId)
        {
            request.OrderIds.Add(orderId);
        }

        if (query.CustomerId is { } customerId)
        {
            request.CustomerIds.Add(customerId);
        }

        using var ordersStream = orderGrpcClient
            .V1QueryOrders(
                request,
                cancellationToken: cancellationToken);

        var orders = new List<CustomerOrder>();
        long? totalCount = null;
        long filteredOrders = 0L;

        try
        {
            while (await ordersStream.ResponseStream.MoveNext(cancellationToken))
            {
                var ordersResponse = ordersStream.ResponseStream.Current;
                totalCount ??= ordersResponse.TotalCount;

                if (string.IsNullOrEmpty(query.Comment) ||
                    ordersResponse.Comment == query.Comment)
                {
                    orders.Add(new CustomerOrder
                    {
                        OrderId = ordersResponse.OrderId,
                        Status = ordersResponse.Status.FromGrpcStatus(),
                        CustomerId = ordersResponse.CustomerId,
                        Comment = ordersResponse.Comment,
                        CreatedAt = ordersResponse.CreatedAt.ToDateTime()
                    });
                }
                else
                {
                    filteredOrders++;
                }
            }
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.InvalidArgument)
        {
            throw new OrderServiceInvalidArgumentException(ex.Message, ex);
        }


        var ordersResult = new OrdersResult(
                Orders: orders,
                TotalCount: totalCount ?? 0L,
                FilteredCount: filteredOrders);

        return ordersResult;
    }
}