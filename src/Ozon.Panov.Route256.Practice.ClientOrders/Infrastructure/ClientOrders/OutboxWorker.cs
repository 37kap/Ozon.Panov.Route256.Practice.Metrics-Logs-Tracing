using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka;
using System.Text.Json;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.ClientOrders;

public sealed class OutboxWorker(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            await Process(stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task Process(CancellationToken stoppingToken)
    {
        try
        {
            using var serviceScope = serviceScopeFactory.CreateScope();
            var outboxRepository = serviceScope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var ordersInputPublisher = serviceScope.ServiceProvider.GetRequiredService<IOrdersPublisher>();

            var messages = await outboxRepository.GetUnprocessedMessages(stoppingToken);

            await SendMessages(messages, outboxRepository, ordersInputPublisher, stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing outbox messages");
        }
    }

    private async Task SendMessages(
        IReadOnlyCollection<OutboxMessage> messages,
        IOutboxRepository outboxRepository,
        IOrdersPublisher ordersInputPublisher,
        CancellationToken stoppingToken)
    {
        foreach (var message in messages)
        {
            await SendSingleMessage(
                message,
                outboxRepository,
                ordersInputPublisher,
                stoppingToken);
        }
    }

    private async Task SendSingleMessage(
        OutboxMessage message,
        IOutboxRepository outboxRepository,
        IOrdersPublisher ordersInputPublisher,
        CancellationToken stoppingToken)
    {
        try
        {
            if (message.Topic == "orders_input")
            {
                var deserializedMessage = JsonSerializer.Deserialize<CreateOrderCommand>(message.Value)!;
                await ordersInputPublisher.Publish(
                    deserializedMessage,
                    stoppingToken);

                await outboxRepository.MarkAsProcessed(message.Id, stoppingToken);
            }
            else
            {
                throw new TopicNotSupportedException(message.Topic);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending outbox message with ID {MessageId}", message.Id);
        }
    }
}