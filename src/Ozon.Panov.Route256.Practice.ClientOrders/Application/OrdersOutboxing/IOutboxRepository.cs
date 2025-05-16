namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;

public interface IOutboxRepository
{
    Task Insert(OutboxMessage message, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OutboxMessage>> GetUnprocessedMessages(CancellationToken cancellationToken);
    Task MarkAsProcessed(Guid messageId, CancellationToken cancellationToken);
}