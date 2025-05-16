using Npgsql;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.DatabaseManagement;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.QueryExecution;

internal sealed class QueryExecutor(NpgsqlConnectionFactory connectionFactory) : IQueryExecutor
{
    public async Task<IReadOnlyList<T>> ExecuteReader<T>(
        string query,
        Func<NpgsqlDataReader, CancellationToken, Task<T>> buildModel,
        CancellationToken token,
        Dictionary<string, object>? param = null)
    {
        var results = new List<T>();

        await using var connection = connectionFactory.GetConnection();
        await using var command = new NpgsqlCommand(query, connection);
        command.CommandTimeout = 5;

        EnrichWithParameters(command, param);

        await connection.OpenAsync(token);
        await using var reader = await command.ExecuteReaderAsync(token);

        while (await reader.ReadAsync(token))
        {
            var result = await buildModel(reader, token);
            results.Add(result);
        }

        return results;
    }

    public async Task ExecuteNonQuery(
        string query, Dictionary<string, object> parameters, CancellationToken token)
    {
        await using var connection = connectionFactory.GetConnection();
        await using var command = new NpgsqlCommand(query, connection);
        command.CommandTimeout = 5;

        EnrichWithParameters(command, parameters);

        await connection.OpenAsync(token);
        await command.ExecuteNonQueryAsync(token);
    }

    private static void EnrichWithParameters(
        NpgsqlCommand command,
        IReadOnlyDictionary<string, object>? parameters)
    {
        if (parameters is null)
        {
            return;
        }

        foreach (var parameter in parameters)
        {
            command.Parameters.AddWithValue(parameter.Key, parameter.Value);
        }
    }
}