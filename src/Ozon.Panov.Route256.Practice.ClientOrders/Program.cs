using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.DatabaseManagement;
using Ozon.Panov.Route256.Practice.ClientOrders.Presentation;
using Prometheus;
using Serilog;

namespace Ozon.Panov.Route256.Practice.ClientOrders;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        IConfigurationRoot configuration = builder.Configuration
            .AddEnvironmentVariables("ROUTE256_")
            .Build();

        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));

        builder.Services
            .AddApplication()
            .AddInfrastructure(configuration)
            .AddPresentation();

        WebApplication app = builder.Build();

        app.UseMetricServer(port: 5001, url: "/metrics");
        app.UseSerilogRequestLogging();
        app.MapPrometheusScrapingEndpoint();
        app.MigrateDatabase();
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseRouting();
        app.MapGrpcService<ClientOrdersGrpcService>();
        app.MapGrpcReflectionService();

        app.Run();
    }
}
