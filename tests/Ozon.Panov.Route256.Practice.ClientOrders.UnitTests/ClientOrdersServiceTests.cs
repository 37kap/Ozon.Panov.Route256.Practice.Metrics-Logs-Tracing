using FluentAssertions;
using Moq;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Customers;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Metrics;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;
using Ozon.Panov.Route256.Practice.ClientOrders.Domain;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Customers;

namespace Ozon.Panov.Route256.Practice.ClientOrders.UnitTests;

public class ClientOrdersServiceTests
{
    private readonly Mock<ICustomersProvider> _customersProviderMock;
    private readonly Mock<ICustomerRegionCache> _customerRegionRepositoryMock;
    private readonly Mock<IOutboxService> _outboxServiceMock;
    private readonly Mock<IClientOrderRepository> _clientOrderRepositoryMock;
    private readonly Mock<IOrderMetrics> _metricsMock;

    private readonly ClientOrdersService _service;

    public ClientOrdersServiceTests()
    {
        _customersProviderMock = new Mock<ICustomersProvider>();
        _customerRegionRepositoryMock = new Mock<ICustomerRegionCache>();
        _outboxServiceMock = new Mock<IOutboxService>();
        _clientOrderRepositoryMock = new Mock<IClientOrderRepository>();
        _metricsMock = new Mock<IOrderMetrics>();

        var cachedCustomersProvider = new CachedCustomersProvider(
            _customersProviderMock.Object,
            _customerRegionRepositoryMock.Object);

        _service = new ClientOrdersService(
            cachedCustomersProvider,
            _outboxServiceMock.Object,
            _clientOrderRepositoryMock.Object,
            _metricsMock.Object);
    }

    [Fact]
    public async Task Should_create_order_when_valid_input()
    {
        // Arrange
        long customerId = 123;
        var items = new[] { new OrderItem("barcode1", 2) };
        long regionId = 1;

        _customerRegionRepositoryMock
            .Setup(x => x.FindCustomerRegion(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((long?)null);

        _customersProviderMock
            .Setup(x => x.GetCustomerRegion(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(regionId);

        CreateOrderCommand? capturedMessage = null;
        _outboxServiceMock
            .Setup(x => x.CreateOutboxMessage(
                It.IsAny<long>(), 
                It.IsAny<CreateOrderCommand>(), 
                It.IsAny<CancellationToken>()))
            .Callback<long, CreateOrderCommand, 
                CancellationToken>((_, message, _) => capturedMessage = message)
            .Returns(Task.CompletedTask);

        // Act
        await _service.CreateOrder(customerId, items, CancellationToken.None);

        // Assert
        _outboxServiceMock.Verify(
            x => x.CreateOutboxMessage(
                customerId, 
                It.IsAny<CreateOrderCommand>(), 
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.NotNull(capturedMessage);
        Assert.Equal(customerId, capturedMessage.CustomerId);
        Assert.Equal(regionId, capturedMessage.RegionId);
        Assert.Equal(items, capturedMessage.Items);
        Assert.False(string.IsNullOrWhiteSpace(capturedMessage.Comment));

        _clientOrderRepositoryMock.Verify(
            x => x.Insert(
                It.IsAny<CustomerOrder>(),
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Should_return_orders_when_orders_exist()
    {
        // Arrange
        long customerId = 123;
        int limit = 10;
        int offset = 0;

        var clientOrders = new[]
        {
            new CustomerOrder
            {
                OrderId = 1,
                Comment = "Test",
                CustomerId = customerId,
                Status = OrderStatus.New,
                CreatedAt = DateTime.UtcNow
            }
        };

        _clientOrderRepositoryMock
            .Setup(x => x.GetByCustomerId(
                customerId, limit, offset, It.IsAny<CancellationToken>()))
            .ReturnsAsync(clientOrders);

        // Act
        var result = await _service.GetCustomerOrders(
            customerId, limit, offset, CancellationToken.None);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().HaveCount(1);
    }
}