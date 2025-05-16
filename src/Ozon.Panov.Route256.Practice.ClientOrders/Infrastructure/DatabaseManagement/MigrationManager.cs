using FluentMigrator.Runner;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.DatabaseManagement;

internal static class MigrationManager
{
    public static IHost MigrateDatabase(
        this IHost host)
    {
        using var scope = host.Services.CreateScope();

        var migrationService = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            migrationService.ListMigrations();
            migrationService.MigrateUp();
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occurred while migrating the database");
            throw;
        }

        return host;
    }
}