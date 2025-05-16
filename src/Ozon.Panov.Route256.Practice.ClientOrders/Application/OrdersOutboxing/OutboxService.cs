using System.Text.Json;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;

internal sealed class OutboxService(IOutboxRepository outboxRepository) : IOutboxService
{
    public async Task CreateOutboxMessage(
        long customerId,
        CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        const string ordersInputTopic = "orders_input";

        var messageValue = JsonSerializer.Serialize(command);

        var outboxMessage = new OutboxMessage(
            Id: Guid.NewGuid(),
            Topic: ordersInputTopic,
            Key: customerId.ToString(),
            Value: messageValue,
            CreatedAt: DateTime.UtcNow,
            IsProcessed: false
        );

        await outboxRepository.Insert(outboxMessage, cancellationToken);
    }
}