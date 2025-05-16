using Confluent.Kafka;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Configuration;

internal sealed class ProducerSettings
{
    public Acks Acks { get; set; } = Acks.Leader;

    public bool EnableIdempotence { get; set; } = false;
}