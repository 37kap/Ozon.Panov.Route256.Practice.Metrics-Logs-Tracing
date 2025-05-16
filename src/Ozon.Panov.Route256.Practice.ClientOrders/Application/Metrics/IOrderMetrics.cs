namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;

public interface IOrderMetrics
{
    void IncrementOrdersCreated(long customerId);
    void IncrementOrderError(string errorType);
    void RecordGrpcRequestTime(double seconds, string methodName);
    void RecordOrderCreationTime(double seconds, long customerId);
    void IncrementCacheSize();
    void IncrementKafkaMessagesInProgress();
    void DecrementKafkaMessagesInProgress();
}