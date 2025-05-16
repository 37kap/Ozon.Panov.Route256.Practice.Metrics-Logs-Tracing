using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Domain;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Proto.ClientOrdersGrpc;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Presentation;

public sealed class ClientOrdersGrpcService(
    IClientOrdersService clientOrdersService) :
    ClientOrdersGrpc.ClientOrdersGrpcBase
{
    public override async Task<V1CreateOrderResponse> V1CreateOrder(
        V1CreateOrderRequest request, ServerCallContext context)
    {
        ValidateCreateOrderRequest(request);

        await clientOrdersService.CreateOrder(
            customerId: request.CustomerId,
            items: [.. request
                .Items
                .Select(item => new OrderItem(
                    Barcode: item.Barcode,
                    Quantity: item.Quantity))],
            cancellationToken: context.CancellationToken);

        return new V1CreateOrderResponse();
    }

    public override async Task V1QueryCustomerOrders(
        V1QueryCustomerOrdersRequest request,
        IServerStreamWriter<V1QueryCustomerOrdersResponse> responseStream,
        ServerCallContext context)
    {
        ValidateQueryCustomerOrdersRequest(request);

        var orders = await clientOrdersService
            .GetCustomerOrders(
                customerId: request.CustomerId,
                limit: request.Limit,
                offset: request.Offset,
                cancellationToken: context.CancellationToken);

        foreach (var order in orders)
        {
            var response = new V1QueryCustomerOrdersResponse
            {
                OrderId = order.OrderId,
                OrderStatus = order.Status.ToGrpcStatus(),
                CreatedAt = Timestamp.FromDateTime(order.OrderDate.ToUniversalTime())
            };
            await responseStream.WriteAsync(response);
        }
    }

    private static void ValidateCreateOrderRequest(V1CreateOrderRequest request)
    {
        if (request.CustomerId <= 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    detail: "CustomerId must be greater than 0."));
        }
        if (request.Items.Count == 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    detail: "Items must not be empty."));
        }
    }

    private static void ValidateQueryCustomerOrdersRequest(V1QueryCustomerOrdersRequest request)
    {
        if (request.CustomerId <= 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    detail: "CustomerId must be greater than 0."));
        }

        if (request.Limit < 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    detail: "Limit must be greater than or equal to 0."));
        }

        if (request.Offset < 0)
        {
            throw new RpcException(
                new Status(
                    StatusCode.FailedPrecondition,
                    detail: "Offset must be greater than or equal to 0."));
        }
    }
}