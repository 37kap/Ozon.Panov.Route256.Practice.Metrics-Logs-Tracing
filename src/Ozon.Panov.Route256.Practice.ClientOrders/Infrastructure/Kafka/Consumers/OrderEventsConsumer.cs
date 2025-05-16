using Confluent.Kafka;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders.OrdersProviding;
using Ozon.Panov.Route256.Practice.ClientOrders.Domain;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Configuration;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Serializers;
using Ozon.Route256.OrderService.Proto.Messages;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Consumers;

internal sealed class OrderEventsConsumer : BackgroundService
{
    private readonly ILogger<OrderEventsConsumer> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly string _topic;
    private readonly TimeSpan _timeoutForRetry;
    private readonly ConsumerConfig _consumerConfig;
    private readonly IOrderMetrics _metrics;

    public OrderEventsConsumer(
        IServiceProvider serviceProvider,
        KafkaSettings kafkaSettings,
        ConsumerSettings consumerSettings,
        ILogger<OrderEventsConsumer> logger,
        IOrderMetrics metrics)
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

        var consumer = new ConsumerBuilder<Ignore, OrderOutputEventMessage>(_consumerConfig)
            .SetValueDeserializer(new ProtoKafkaSerializer<OrderOutputEventMessage>())
            .Build();

        consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Process(consumer, stoppingToken);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    private async Task Process(
        IConsumer<Ignore, OrderOutputEventMessage> consumer,
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
            var ordersProvider = scope.ServiceProvider.GetRequiredService<IOrdersProvider>();

            await ProcessOrderEventMessage(
                consumeResult.Message.Value,
                clientOrderRepository,
                ordersProvider,
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

    private async Task ProcessOrderEventMessage(
        OrderOutputEventMessage message,
        IClientOrderRepository clientOrderRepository,
        IOrdersProvider ordersProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            _metrics.IncrementKafkaMessagesInProgress();

            switch (message.EventType)
            {
                case OutputEventType.Created:
                case OutputEventType.Updated:
                    await HandleEvent(message, clientOrderRepository, ordersProvider, cancellationToken);
                    break;
            }
        }
        finally
        {
            _metrics.DecrementKafkaMessagesInProgress();
        }
    }

    private async Task HandleEvent(
        OrderOutputEventMessage message,
        IClientOrderRepository clientOrderRepository,
        IOrdersProvider ordersProvider,
        CancellationToken cancellationToken)
    {
        var orderFromProvider = await GetOrderFromProvider(
            message.OrderId,
            ordersProvider,
            cancellationToken);

        if (orderFromProvider.Comment is not string comment)
            return;

        var clientOrder = await clientOrderRepository.GetByComment(comment, cancellationToken);
        if (clientOrder?.CustomerId != orderFromProvider.CustomerId)
            return;

        _logger.LogError("Order {OrderId} event was received: {EventType}",
            message.OrderId,
            message.EventType.ToString("G"));

        if (message.EventType == OutputEventType.Created)
        {
            clientOrder.OrderId = orderFromProvider.OrderId;

            _metrics.IncrementOrdersCreated(clientOrder.CustomerId);
        }

        clientOrder.Status = orderFromProvider.Status;

        await clientOrderRepository.Update(clientOrder, cancellationToken);
    }

    private static async Task<CustomerOrder> GetOrderFromProvider(
        long orderId,
        IOrdersProvider ordersProvider,
        CancellationToken cancellationToken)
    {
        var ordersResult = await ordersProvider.GetOrders(
            new OrdersQuery(
                Limit: 1,
                Offset: 0,
                OrderId: orderId),
            cancellationToken);

        if (ordersResult.Orders.FirstOrDefault() is { } order)
        {
            return order;
        }

        throw new OrderNotFoundInProviderException(orderId);
    }
}