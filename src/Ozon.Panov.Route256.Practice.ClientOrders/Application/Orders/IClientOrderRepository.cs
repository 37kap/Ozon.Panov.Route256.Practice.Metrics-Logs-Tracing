using Ozon.Panov.Route256.Practice.ClientOrders.Domain;

namespace Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;

public interface IClientOrderRepository
{
    Task Insert(
        CustomerOrder order,
        CancellationToken cancellationToken);

    Task<CustomerOrder?> GetByComment(
        string comment,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerOrder>> GetByCustomerId(
        long customerId,
        int limit,
        int offset,
        CancellationToken cancellationToken);

    Task Update(
        CustomerOrder order,
        CancellationToken cancellationToken);
}
