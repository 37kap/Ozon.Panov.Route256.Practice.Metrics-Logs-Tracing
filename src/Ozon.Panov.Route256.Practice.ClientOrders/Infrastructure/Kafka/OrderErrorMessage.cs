namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka;

internal sealed record OrderErrorMessage(
    string Comment,
    string ErrorText);