using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;

public class OrderMetrics : IOrderMetrics, IDisposable
{
    public const string MetricName = "client-orders-metrics";

    private readonly Meter _meter;

    private readonly Counter<long> _ordersCreatedCounter;
    private readonly Counter<long> _orderErrorsCounter;

    private readonly Histogram<double> _orderCreationDuration;
    private readonly Histogram<double> _grpcRequestDuration;

    private readonly ObservableGauge<long> _regionCacheSizeGauge;
    private long _currentCacheSize;

    private readonly ObservableGauge<int> _kafkaMessagesInProgress;
    private int _currentMessagesInProgress;

    public OrderMetrics()
    {
        _meter = new Meter(MetricName, "1.0.0");

        _ordersCreatedCounter = _meter.CreateCounter<long>(
            name: "orders.created.total",
            unit: "{order}",
            description: "Количество созданных заказов");

        _orderErrorsCounter = _meter.CreateCounter<long>(
            name: "orders.errors.total",
            unit: "{error}",
            description: "Количество ошибок при обработке заказов");

        _orderCreationDuration = _meter.CreateHistogram<double>(
            name: "order.creation.duration",
            unit: "s",
            description: "Время создания заказа");

        _grpcRequestDuration = _meter.CreateHistogram<double>(
            name: "grpc.request.duration",
            unit: "s",
            description: "Время выполнения gRPC запросов");

        _regionCacheSizeGauge = _meter.CreateObservableGauge<long>(
            name: "region.cache.size",
            observeValue: () => _currentCacheSize,
            unit: "{item}",
            description: "Количество записей о регионах пользователей в кэше");

        _kafkaMessagesInProgress = _meter.CreateObservableGauge<int>(
            name: "kafka.messages.in_progress",
            observeValue: () => _currentMessagesInProgress,
            unit: "{message}",
            description: "Количество сообщений в Kafka, которые обрабатываются в данный момент");
    }

    public void IncrementOrdersCreated(long customerId)
    {
        var tags = new TagList
        {
            { "customer_id", customerId }
        };

        _ordersCreatedCounter.Add(1, tags);
    }

    public void IncrementOrderError(string errorType)
    {
        var tags = new TagList { { "error_type", errorType } };
        _orderErrorsCounter.Add(1, tags);
    }

    public void RecordOrderCreationTime(double seconds, long customerId)
    {
        var tags = new TagList { { "customer_id", customerId } };
        _orderCreationDuration.Record(seconds, tags);
    }

    public void RecordGrpcRequestTime(double seconds, string methodName)
    {
        var tags = new TagList { { "method", methodName } };
        _grpcRequestDuration.Record(seconds, tags);
    }

    public void IncrementCacheSize() => Interlocked.Increment(ref _currentCacheSize);

    public void IncrementKafkaMessagesInProgress()
    {
        Interlocked.Increment(ref _currentMessagesInProgress);
    }

    public void DecrementKafkaMessagesInProgress()
    {
        Interlocked.Decrement(ref _currentMessagesInProgress);
    }

    public void Dispose() => _meter?.Dispose();
}