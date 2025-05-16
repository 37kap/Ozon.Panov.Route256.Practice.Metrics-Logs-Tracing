using Npgsql;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Domain;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.QueryExecution;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.ClientOrders;

internal sealed class ClientOrderRepository(
    IQueryExecutor queryExecutor) :
    IClientOrderRepository
{
    public async Task Insert(CustomerOrder order, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO client_orders (order_id, comment, customer_id, status, created_at)
            VALUES (@OrderId, @Comment, @CustomerId, @Status, @CreatedAt);
        """;

        var parameters = new Dictionary<string, object>
        {
            { "OrderId", order.OrderId },
            { "Comment", order.Comment },
            { "CustomerId", order.CustomerId },
            { "Status", (int)order.Status },
            { "CreatedAt", order.CreatedAt }
        };

        await queryExecutor.ExecuteNonQuery(sql, parameters, cancellationToken);
    }

    public async Task<CustomerOrder?> GetByComment(
        string comment,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT order_id, comment, customer_id, status, created_at
            FROM client_orders
            WHERE comment = @Comment;
        """;

        var parameters = new Dictionary<string, object>
        {
            { "Comment", comment }
        };

        var orders = await queryExecutor.ExecuteReader(
            sql,
            BuildClientOrder,
            cancellationToken,
            parameters);
        return orders.FirstOrDefault();
    }

    public async Task<IReadOnlyList<CustomerOrder>> GetByCustomerId(
        long customerId,
        int limit,
        int offset,
        CancellationToken cancellationToken)
    {
        string sql = """
            SELECT order_id, comment, customer_id, status, created_at
            FROM client_orders
            WHERE customer_id = @CustomerId
            ORDER BY created_at DESC
        """;

        var parameters = new Dictionary<string, object>
        {
            { "CustomerId", customerId }
        };

        if (limit > 0)
        {
            sql += "\nLIMIT @Limit";
            parameters.Add("Limit", limit);
        }

        if (offset > 0)
        {
            sql += "\nOFFSET @Offset";
            parameters.Add("Offset", offset);
        }

        var orders = await queryExecutor.ExecuteReader(
            sql,
            BuildClientOrder,
            cancellationToken,
            parameters);

        return orders;
    }

    public async Task Update(CustomerOrder order, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE client_orders
            SET order_id = @OrderId,
                customer_id = @CustomerId,
                status = @Status,
                created_at = @CreatedAt
            WHERE comment = @Comment;
        """;

        var parameters = new Dictionary<string, object>
        {
            { "OrderId", order.OrderId },
            { "Comment", order.Comment },
            { "CustomerId", order.CustomerId },
            { "Status", (int)order.Status },
            { "CreatedAt", order.CreatedAt }
        };

        await queryExecutor.ExecuteNonQuery(sql, parameters, cancellationToken);
    }

    private async Task<CustomerOrder> BuildClientOrder(
        NpgsqlDataReader reader,
        CancellationToken cancellationToken)
    {
        return new CustomerOrder
        {
            OrderId = await reader.GetFieldValueAsync<long>(0, cancellationToken),
            Comment = await reader.GetFieldValueAsync<string>(1, cancellationToken),
            CustomerId = await reader.GetFieldValueAsync<long>(2, cancellationToken),
            Status = (OrderStatus)await reader.GetFieldValueAsync<int>(3, cancellationToken),
            CreatedAt = await reader.GetFieldValueAsync<DateTime>(4, cancellationToken)
        };
    }
}
