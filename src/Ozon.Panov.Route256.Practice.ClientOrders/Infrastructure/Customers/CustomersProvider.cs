using Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;
using Ozon.Route256.CustomerService;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Customers;

internal sealed class CustomersProvider(
    CustomerService.CustomerServiceClient customerServiceClient) : ICustomersProvider
{
    public async Task<long> GetCustomerRegion(
        long customerId,
        CancellationToken cancellationToken)
    {
        var request = new V1QueryCustomersRequest
        {
            CustomerIds =
            {
                customerId
            },
            Limit = 1,
            Offset = 0
        };

        using var customersStream =
            customerServiceClient
                .V1QueryCustomers(
                    request,
                    cancellationToken: cancellationToken);

        if (await customersStream.ResponseStream.MoveNext(cancellationToken))
        {
            var customerResponse = customersStream.ResponseStream.Current;

            return customerResponse.Customer.Region.Id;
        }

        throw new CustomerNotFoundInProviderException(customerId);
    }
}