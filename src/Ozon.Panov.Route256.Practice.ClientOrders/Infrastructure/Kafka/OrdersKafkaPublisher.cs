using Confluent.Kafka;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Configuration;
using System.Text.Json;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka;

internal class OrdersKafkaPublisher : IOrdersPublisher, IDisposable
{
    private readonly IProducer<long, string> _producer;
    private readonly ILogger<OrdersKafkaPublisher> _logger;
    private const string Topic = "orders_input";

    public OrdersKafkaPublisher(
        KafkaSettings kafkaSettings,
        ProducerSettings producerSettings,
        ILogger<OrdersKafkaPublisher> logger)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = kafkaSettings.BootstrapServers,
            Acks = producerSettings.Acks,
            EnableIdempotence = producerSettings.EnableIdempotence
        };
        _producer = new ProducerBuilder<long, string>(producerConfig).Build();
        _logger = logger;
    }

    public async Task Publish(
        CreateOrderCommand order,
        CancellationToken token)
    {
        var value = JsonSerializer.Serialize(order);

        try
        {
            var message = new Message<long, string>
            {
                Key = order.CustomerId,
                Value = value
            };

            await _producer.ProduceAsync(Topic, message, token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error in producing to {Topic}", Topic);
            throw;
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _producer.Flush();
        _producer.Dispose();
    }
}