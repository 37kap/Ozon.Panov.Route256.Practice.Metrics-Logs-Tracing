using Npgsql;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.QueryExecution;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.ClientOrders;

internal sealed class OutboxRepository(
    IQueryExecutor queryExecutor) :
    IOutboxRepository
{
    public async Task Insert(OutboxMessage message, CancellationToken cancellationToken)
    {
        const string sql = @"
            INSERT INTO outbox_messages (id, topic, key, value, created_at, is_processed)
            VALUES (@Id, @Topic, @Key, @Value, @CreatedAt, @IsProcessed);
        ";

        var parameters = new Dictionary<string, object>
        {
            { "Id", message.Id },
            { "Topic", message.Topic },
            { "Key", message.Key },
            { "Value", message.Value },
            { "CreatedAt", message.CreatedAt },
            { "IsProcessed", message.IsProcessed }
        };

        await queryExecutor.ExecuteNonQuery(sql, parameters, cancellationToken);
    }

    public async Task<IReadOnlyCollection<OutboxMessage>> GetUnprocessedMessages(CancellationToken cancellationToken)
    {
        const string sql = @"
            SELECT id, topic, key, value, created_at, is_processed, processed_at
            FROM outbox_messages
            WHERE is_processed = FALSE;
        ";

        return await queryExecutor.ExecuteReader(sql, BuildOutboxMessage, cancellationToken);
    }

    public async Task MarkAsProcessed(Guid messageId, CancellationToken cancellationToken)
    {
        const string sql = @"
            UPDATE outbox_messages
            SET is_processed = TRUE, processed_at = @ProcessedAt
            WHERE id = @Id;
        ";

        var parameters = new Dictionary<string, object>
        {
            { "Id", messageId },
            { "ProcessedAt", DateTime.UtcNow }
        };

        await queryExecutor.ExecuteNonQuery(sql, parameters, cancellationToken);
    }

    private async Task<OutboxMessage> BuildOutboxMessage(NpgsqlDataReader reader, CancellationToken cancellationToken)
    {
        return new(
            Id: await reader.GetFieldValueAsync<Guid>(0, cancellationToken),
            Topic: await reader.GetFieldValueAsync<string>(1, cancellationToken),
            Key: await reader.GetFieldValueAsync<string>(2, cancellationToken),
            Value: await reader.GetFieldValueAsync<string>(3, cancellationToken),
            CreatedAt: await reader.GetFieldValueAsync<DateTime>(4, cancellationToken),
            IsProcessed: await reader.GetFieldValueAsync<bool>(5, cancellationToken),
            ProcessedAt: await reader.IsDBNullAsync(6, cancellationToken) ?
                null :
                await reader.GetFieldValueAsync<DateTime>(6, cancellationToken)
        );
    }
}
