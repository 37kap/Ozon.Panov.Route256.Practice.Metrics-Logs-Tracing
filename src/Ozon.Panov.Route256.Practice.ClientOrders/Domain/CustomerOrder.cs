namespace Ozon.Panov.Route256.Practice.ClientOrders.Domain;

public sealed class CustomerOrder
{
    public long OrderId { get; set; }
    public string Comment { get; set; } = null!;
    public long CustomerId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}