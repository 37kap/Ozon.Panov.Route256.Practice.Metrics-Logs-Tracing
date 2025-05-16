namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka;

public sealed class TopicNotSupportedException(string topicName) :
    Exception($"Topic '{topicName}' is not supported yet.");