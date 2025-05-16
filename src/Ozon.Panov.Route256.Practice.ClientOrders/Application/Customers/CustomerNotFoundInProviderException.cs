namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;

internal sealed class CustomerNotFoundInProviderException(long customerId) :
    Exception($"Customer with id {customerId} was not found.");