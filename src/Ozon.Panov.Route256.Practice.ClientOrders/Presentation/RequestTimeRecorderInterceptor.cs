using Grpc.Core;
using Grpc.Core.Interceptors;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;
using System.Diagnostics;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Presentation;

public sealed class RequestTimeRecorderInterceptor(
    IOrderMetrics metrics) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            TResponse response = await base.UnaryServerHandler(request, context, continuation);
            return response;
        }
        finally
        {
            stopwatch.Stop();
            metrics.RecordGrpcRequestTime(stopwatch.Elapsed.TotalSeconds, context.Method);
        }
    }
}