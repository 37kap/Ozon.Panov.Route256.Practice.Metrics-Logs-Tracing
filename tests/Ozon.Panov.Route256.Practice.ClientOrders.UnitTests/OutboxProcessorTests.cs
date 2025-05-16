using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.Orders;
using Ozon.Panov.Route256.Practice.ClientOrders.Application.OrdersOutboxing;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.ClientOrders;
using Ozon.Panov.Route256.Practice.ClientOrders.Infrastructure.Kafka;

namespace Ozon.Panov.Route256.Practice.ClientOrders.UnitTests;


public class OutboxProcessorTests
{
    private readonly Mock<IOrdersPublisher> _ordersPublisherMock;
    private readonly Mock<IOutboxRepository> _outboxRepositoryMock;
    private readonly Mock<ILogger<OutboxWorker>> _loggerMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly OutboxWorker _outboxWorker;

    public OutboxProcessorTests()
    {
        _ordersPublisherMock = new Mock<IOrdersPublisher>();
        _outboxRepositoryMock = new Mock<IOutboxRepository>();
        _loggerMock = new Mock<ILogger<OutboxWorker>>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();

        var serviceScopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        serviceProviderMock.Setup(sp => sp.GetService(typeof(IOutboxRepository)))
            .Returns(_outboxRepositoryMock.Object);
        serviceProviderMock.Setup(sp => sp.GetService(typeof(IOrdersPublisher)))
            .Returns(_ordersPublisherMock.Object);

        serviceScopeMock.Setup(s => s.ServiceProvider)
            .Returns(serviceProviderMock.Object);

        _serviceScopeFactoryMock.Setup(f => f.CreateScope())
            .Returns(serviceScopeMock.Object);

        _outboxWorker = new OutboxWorker(
            _serviceScopeFactoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Worker_should_process_unprocessed_messages()
    {
        // Arrange
        var outboxMessage = new OutboxMessage
        (
            Id: Guid.NewGuid(),
            Topic: "orders_input",
            Key: "123",
            Value: "{}",
            CreatedAt: DateTime.UtcNow,
            IsProcessed: false
        );

        _outboxRepositoryMock
            .Setup(x => x.GetUnprocessedMessages(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { outboxMessage });

        _ordersPublisherMock
            .Setup(x => x.Publish(
                It.IsAny<CreateOrderCommand>(), 
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _outboxRepositoryMock
            .Setup(x => x.MarkAsProcessed(outboxMessage.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _outboxWorker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await _outboxWorker.StopAsync(CancellationToken.None);

        // Assert
        _ordersPublisherMock.Verify(x => x.Publish(
            It.IsAny<CreateOrderCommand>(),
            It.IsAny<CancellationToken>()), 
            Times.Once);
        _outboxRepositoryMock.Verify(x => x.MarkAsProcessed(
            outboxMessage.Id, 
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_log_error_when_exception_occurs()
    {
        // Arrange
        var outboxMessage = new OutboxMessage
        (
            Id: Guid.NewGuid(),
            Topic: "unsupported_topic",
            Key: "123",
            Value: "{}",
            CreatedAt: DateTime.UtcNow,
            IsProcessed: false
        );

        _outboxRepositoryMock
            .Setup(x => x.GetUnprocessedMessages(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OutboxMessage> { outboxMessage });

        // Act
        await _outboxWorker.StartAsync(CancellationToken.None);
        await Task.Delay(100);
        await _outboxWorker.StopAsync(CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<TopicNotSupportedException>(),
                (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()),
            Times.AtLeastOnce);
    }
}