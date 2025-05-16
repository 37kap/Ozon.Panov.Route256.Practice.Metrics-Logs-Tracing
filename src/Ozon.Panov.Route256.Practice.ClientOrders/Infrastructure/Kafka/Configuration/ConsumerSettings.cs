namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Configuration;

internal sealed class ConsumerSettings
{
    public string Topic { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public bool AutoCommit { get; set; } = false;
}