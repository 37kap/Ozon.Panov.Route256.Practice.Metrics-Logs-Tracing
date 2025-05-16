namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Configuration;

internal sealed class KafkaSettings
{
    public string BootstrapServers { get; set; } = string.Empty;

    public string GroupId { get; set; } = string.Empty;

    public int TimeoutForRetryInSeconds { get; set; } = 2;
}