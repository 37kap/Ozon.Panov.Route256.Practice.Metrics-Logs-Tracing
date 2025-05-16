using Confluent.Kafka;
using Google.Protobuf;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Serializers;

public class ProtoKafkaSerializer<TMessage> :
    IDeserializer<TMessage>, ISerializer<TMessage> where TMessage : IMessage<TMessage>, new()
{
    private static readonly MessageParser<TMessage> _parser = new(() => new TMessage());

    public TMessage Deserialize(ReadOnlySpan<byte> data, bool isNull, SerializationContext context)
    {
        return _parser.ParseFrom(data);
    }

    public byte[] Serialize(TMessage data, SerializationContext context)
    {
        return data.ToByteArray();
    }
}