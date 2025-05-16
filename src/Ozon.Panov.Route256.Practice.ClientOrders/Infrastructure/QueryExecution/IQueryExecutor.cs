using Npgsql;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.QueryExecution;

public interface IQueryExecutor
{
    Task<IReadOnlyList<T>> ExecuteReader<T>(
        string query,
        Func<NpgsqlDataReader, CancellationToken, Task<T>> buildModel,
        CancellationToken token,
        Dictionary<string, object>? param = null);

    Task ExecuteNonQuery(
        string query, Dictionary<string, object> parameters, CancellationToken token);
}
