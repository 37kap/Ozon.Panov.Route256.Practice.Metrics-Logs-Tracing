using Npgsql;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.DatabaseManagement;

internal sealed class NpgsqlConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlConnectionFactory(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        _dataSource = dataSourceBuilder.Build();
    }

    public NpgsqlConnection GetConnection()
    {
        return _dataSource.CreateConnection();
    }
}