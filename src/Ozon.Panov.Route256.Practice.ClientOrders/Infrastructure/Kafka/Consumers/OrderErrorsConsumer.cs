using Confluent.Kafka;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Domain;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Configuration;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Serializers;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Consumers;

internal sealed class OrderErrorsConsumer : BackgroundService
{
    private readonly ILogger<OrderErrorsConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _topic;
    private readonly TimeSpan _timeoutForRetry;
    private readonly ConsumerConfig _consumerConfig;
    private readonly IOrderMetrics _metrics;

    public OrderErrorsConsumer(
        IServiceProvider serviceProvider,
        KafkaSettings kafkaSettings,
        ConsumerSettings consumerSettings,
        IOrderMetrics metrics,
        ILogger<OrderErrorsConsumer> logger)
    {
        _metrics = metrics;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _topic = consumerSettings.Topic;
        _timeoutForRetry = TimeSpan.FromSeconds(kafkaSettings.TimeoutForRetryInSeconds);
        _consumerConfig = new ConsumerConfig
        {
            GroupId = kafkaSettings.GroupId,
            BootstrapServers = kafkaSettings.BootstrapServers,
            EnableAutoCommit = consumerSettings.AutoCommit
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        var consumer = new ConsumerBuilder<long, OrderErrorMessage>(_consumerConfig)
            .SetKeyDeserializer(Deserializers.Int64)
            .SetValueDeserializer(new JsonKafkaSerializer<OrderErrorMessage>())
            .Build();

        consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Process(consumer, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task Process(
        IConsumer<long, OrderErrorMessage> consumer,
        CancellationToken stoppingToken)
    {
        try
        {
            if (consumer.Consume(stoppingToken) is not { } consumeResult)
            {
                return;
            }

            await using var scope = _serviceProvider.CreateAsyncScope();
            var clientOrderRepository = scope.ServiceProvider.GetRequiredService<IClientOrderRepository>();

            await ProcessOrderErrorMessage(
                consumeResult.Message.Value,
                clientOrderRepository,
                stoppingToken);

            consumer.Commit(consumeResult);
        }
        catch (ConsumeException e)
        {
            _logger.LogError(e, "Consume error: {Reason}", e.Error.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            await Task.Delay(_timeoutForRetry, stoppingToken);
        }
    }

    private async Task ProcessOrderErrorMessage(
        OrderErrorMessage errorMessage,
        IClientOrderRepository clientOrderRepository,
        CancellationToken cancellationToken)
    {
        if (await clientOrderRepository
                .GetByComment(errorMessage.Comment, cancellationToken) is not { } clientOrder)
        {
            return;
        }

        _logger.LogError("Order input error was received: {Error}", errorMessage.ErrorText);
        clientOrder.Status = OrderStatus.Undefined;

        await clientOrderRepository.Update(clientOrder, cancellationToken);
        _metrics.IncrementOrderError(errorMessage.ErrorText);
    }
}