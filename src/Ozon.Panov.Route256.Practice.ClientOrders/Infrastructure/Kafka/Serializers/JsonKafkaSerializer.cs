using Confluent.Kafka;
using System.Text.Json;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Serializers;

public sealed class JsonKafkaSerializer<TMessage> : IDeserializer<TMessage>
{
    public TMessage Deserialize(
        ReadOnlySpan<byte> data,
        bool isNull,
        SerializationContext context)
    {
        return JsonSerializer.Deserialize<TMessage>(data)!;
    }
}