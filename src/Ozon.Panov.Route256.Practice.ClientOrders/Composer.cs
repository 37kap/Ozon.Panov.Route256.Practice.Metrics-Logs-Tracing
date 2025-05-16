using FluentMigrator.Runner;
using Grpc.Net.Client.Configuration;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders.OrdersProviding;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.ClientOrders;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Customers;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.DatabaseManagement;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Configuration;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka.Consumers;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.QueryExecution;
using Ozon.Panov.Route256.Practice.ClientOrders.Presentation;
using Ozon.Route256.CustomerService;
using Ozon.Route256.OrderService.Proto.OrderGrpc;
using StackExchange.Redis;
using System.Reflection;

namespace Ozon.Panov.Route256.Practice.ClientOrders;

public static class Composer
{
    public static IServiceCollection AddPresentation(
        this IServiceCollection services)
    {
        services
            .AddGrpc(options =>
            {
                options.EnableDetailedErrors = true;
                options.Interceptors.Add<GrpcExceptionInterceptor>();
                options.Interceptors.Add<RequestTimeRecorderInterceptor>();
            })
            .AddJsonTranscoding();
        services.AddGrpcSwagger();
        services.AddSwaggerGen(c =>
        {
            string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });
        services.AddGrpcReflection();

        return services;
    }

    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        return services
            .AddScoped<IOutboxService, OutboxService>()
            .AddScoped<IClientOrdersService, ClientOrdersService>();
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetValue<string>("CLIENT_ORDER_SERVICE_DB_CONNECTION_STRING")!;

        services
            .AddMetric(configuration)
            .AddTracing(configuration);

        return services
            .AddGrpcClients(configuration)
            .AddMigration(connectionString)
            .AddHostedService<OutboxWorker>()
            .AddSingleton(_ => new NpgsqlConnectionFactory(connectionString))
            .AddSingleton<IQueryExecutor, QueryExecutor>()
            .AddScoped<IOutboxRepository, OutboxRepository>()
            .AddScoped<IClientOrderRepository, ClientOrderRepository>()
            .AddScoped<CustomersProvider>()
            .AddScoped<ICustomersProvider>(provider =>
            {
                var customersProvider = provider.GetRequiredService<CustomersProvider>();
                var cache = provider.GetRequiredService<ICustomerRegionCache>();
                return new CachedCustomersProvider(customersProvider, cache);
            })
            .AddScoped<IOrdersProvider, OrdersProvider>()
            .AddRedis(configuration)
            .AddKafka(configuration);
    }

    public static OpenTelemetryBuilder AddMetric(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddSingleton<IOrderMetrics, OrderMetrics>()
            .AddOpenTelemetry()
            .WithMetrics(metrics =>
            {
                metrics.SetResourceBuilder(ResourceBuilder
                    .CreateDefault()
                    .AddService("ClientOrdersService"));

                metrics.AddMeter(OrderMetrics.MetricName);
                metrics.AddPrometheusExporter();

                metrics.AddProcessInstrumentation();
                metrics.AddRuntimeInstrumentation();
                metrics.AddAspNetCoreInstrumentation();

                metrics.AddMeter("Microsoft.AspNetCore.Hosting");
                metrics.AddMeter("Microsoft.AspNetCore.Server.Kestrel");
            });
    }

    public static OpenTelemetryBuilder AddTracing(
        this OpenTelemetryBuilder services,
        IConfiguration configuration)
    {
        var jaeger = configuration.GetValue<string>("JAEGER_ADDRESS")!;
        return services.WithTracing(trace =>
        {
            trace
                .SetResourceBuilder(ResourceBuilder
                    .CreateDefault()
                    .AddService("ClientOrdersService")
                    .AddAttributes(new Dictionary<string, object> { { "from", "docker" } }))
                .AddNpgsql()
                .AddAspNetCoreInstrumentation()
                .SetSampler(new AlwaysOnSampler())
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(jaeger));
        });
    }

    private static IServiceCollection AddMigration(this IServiceCollection services, string connectionString)
    {
        return services.AddLogging(c => c.AddFluentMigratorConsole())
            .AddFluentMigratorCore()
            .ConfigureRunner(
                x => x.AddPostgres()
                    .WithGlobalConnectionString(connectionString)
                    .ScanIn(Assembly.GetExecutingAssembly())
                    .For.Migrations());
    }

    private static IServiceCollection AddRedis(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSingleton<IConnectionMultiplexer>(_ =>
            {
                var connectionString = configuration.GetValue<string>("CLIENT_ORDER_SERVICE_REDIS_CONNECTION_STRING")!;
                var options = ConfigurationOptions.Parse(connectionString, ignoreUnknown: true);
                options.ResolveDns = false;
                options.AbortOnConnectFail = false;
                options.ConnectRetry = 5;
                options.ConnectTimeout = 5000;
                return ConnectionMultiplexer.Connect(options);
            })
            .AddScoped<RedisDatabaseFactory>()
            .AddScoped<ICustomerRegionCache, CustomerRegionRedisRepository>();
    }

    private static IServiceCollection AddGrpcClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddCustomerServiceGrpcClient(configuration)
            .AddOrderServiceGrpcClient(configuration);
    }

    private static IServiceCollection AddCustomerServiceGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var customerServiceUrl = configuration.GetValue<string>("CUSTOMER_SERVICE_URL");

        services.AddGrpcClient<CustomerService.CustomerServiceClient>(options =>
        {
            options.Address = new Uri(customerServiceUrl!);
        })
            .ConfigureChannel(options =>
            {
                options.ServiceConfig = new ServiceConfig
                {
                    MethodConfigs = { GetDefaultMethodConfig() }
                };
            });

        return services;
    }

    private static IServiceCollection AddOrderServiceGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var orderServiceUrl = configuration.GetValue<string>("ORDER_SERVICE_URL");

        services.AddGrpcClient<OrderGrpc.OrderGrpcClient>(options =>
        {
            options.Address = new Uri(orderServiceUrl!);
        })
            .ConfigureChannel(options =>
            {
                options.ServiceConfig = new ServiceConfig
                {
                    MethodConfigs = { GetDefaultMethodConfig() }
                };
            });

        return services;
    }

    private static MethodConfig GetDefaultMethodConfig()
    {
        return new MethodConfig
        {
            Names = { MethodName.Default },
            RetryPolicy = new RetryPolicy
            {
                MaxAttempts = 5,
                InitialBackoff = TimeSpan.FromMilliseconds(10),
                MaxBackoff = TimeSpan.FromMilliseconds(25),
                BackoffMultiplier = 1.5,
                RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable }
            }
        };
    }

    private static IServiceCollection AddKafka(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var kafkaSettings = configuration.GetSection("Kafka").Get<KafkaSettings>()!;
        var bootstrapServers = configuration.GetValue<string>("KAFKA_BROKERS")!;
        var producerSettings = configuration.GetSection("Kafka:Producer").Get<ProducerSettings>()!;

        kafkaSettings.BootstrapServers = bootstrapServers;

        return services
            .AddProducer(producerSettings, kafkaSettings)
            .AddConsumers(configuration, kafkaSettings);
    }

    private static IServiceCollection AddConsumers(
        this IServiceCollection services,
        IConfiguration configuration,
        KafkaSettings kafkaSettings)
        => services
        .AddHostedService(serviceProvider =>
            new OrderEventsConsumer(
                serviceProvider,
                kafkaSettings,
                configuration
                    .GetSection("Kafka:Consumer:OrderEventsConsumer")
                    .Get<ConsumerSettings>()!,
                serviceProvider.GetRequiredService<ILogger<OrderEventsConsumer>>(),
                serviceProvider.GetRequiredService<IOrderMetrics>()))
        .AddHostedService(serviceProvider =>
            new OrderErrorsConsumer(
                serviceProvider,
                kafkaSettings,
                configuration
                    .GetSection("Kafka:Consumer:OrderErrorsConsumer")
                    .Get<ConsumerSettings>()!,
                serviceProvider.GetRequiredService<IOrderMetrics>(),
                serviceProvider.GetRequiredService<ILogger<OrderErrorsConsumer>>()));

    private static IServiceCollection AddProducer(
        this IServiceCollection services,
        ProducerSettings producerSettings,
        KafkaSettings kafkaSettings)
        => services.AddSingleton<IOrdersPublisher, OrdersKafkaPublisher>(
            serviceProvider => new OrdersKafkaPublisher(
                kafkaSettings,
                producerSettings,
                serviceProvider.GetRequiredService<ILogger<OrdersKafkaPublisher>>()));
}