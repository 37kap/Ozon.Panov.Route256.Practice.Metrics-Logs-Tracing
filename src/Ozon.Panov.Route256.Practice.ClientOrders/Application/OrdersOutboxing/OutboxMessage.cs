namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;

public sealed record OutboxMessage(Guid Id, string Topic, string Key, string Value,
    DateTime CreatedAt, bool IsProcessed, DateTime? ProcessedAt = null);