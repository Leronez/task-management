using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskManagement.Consumer.Workers;
using Xunit;

namespace TaskManagement.Tests.Consumer;

public class TaskEventConsumerTests
{
    private readonly Mock<ILogger<TaskEventConsumer>> _loggerMock = new();
    private readonly Mock<IModel> _channelMock = new();
    private readonly TaskEventConsumer _sut;

    public TaskEventConsumerTests()
    {
        _sut = new TaskEventConsumer(_loggerMock.Object, Mock.Of<IConfiguration>());
    }

    private static BasicDeliverEventArgs MakeDelivery(
        string json,
        ulong deliveryTag = 1,
        IDictionary<string, object>? headers = null)
    {
        var props = new Mock<IBasicProperties>();
        props.Setup(p => p.Headers).Returns(headers!);
        return new BasicDeliverEventArgs
        {
            DeliveryTag = deliveryTag,
            BasicProperties = props.Object,
            Body = Encoding.UTF8.GetBytes(json)
        };
    }

    // ── Logging ───────────────────────────────────────────────────────────────

    [Fact]
    public void HandleMessage_LogsReceivedJson()
    {
        var ea = MakeDelivery("""{"TaskId":1,"EventType":"TaskCreated"}""");

        _sut.HandleMessage(_channelMock.Object, ea);

        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("TaskCreated")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // ── BasicAck ─────────────────────────────────────────────────────────────

    [Fact]
    public void HandleMessage_AcksWithCorrectDeliveryTag()
    {
        var ea = MakeDelivery("{}", deliveryTag: 42);

        _sut.HandleMessage(_channelMock.Object, ea);

        _channelMock.Verify(c => c.BasicAck(42UL, false), Times.Once);
    }

    [Fact]
    public void HandleMessage_NeverAcksWithMultipleFlag()
    {
        var ea = MakeDelivery("{}");

        _sut.HandleMessage(_channelMock.Object, ea);

        _channelMock.Verify(c => c.BasicAck(It.IsAny<ulong>(), true), Times.Never);
    }

    // ── Header parsing ────────────────────────────────────────────────────────

    [Fact]
    public void HandleMessage_NullHeaders_StillAcks()
    {
        var ea = MakeDelivery("{}", headers: null);

        _sut.HandleMessage(_channelMock.Object, ea);

        _channelMock.Verify(c => c.BasicAck(It.IsAny<ulong>(), false), Times.Once);
    }

    [Fact]
    public void HandleMessage_EmptyHeaders_StillAcks()
    {
        var ea = MakeDelivery("{}", headers: new Dictionary<string, object>());

        _sut.HandleMessage(_channelMock.Object, ea);

        _channelMock.Verify(c => c.BasicAck(It.IsAny<ulong>(), false), Times.Once);
    }

    [Fact]
    public void HandleMessage_WithValidTraceparent_StillAcks()
    {
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = Encoding.UTF8.GetBytes("00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01")
        };
        var ea = MakeDelivery("{}", headers: headers);

        _sut.HandleMessage(_channelMock.Object, ea);

        _channelMock.Verify(c => c.BasicAck(It.IsAny<ulong>(), false), Times.Once);
    }

    [Fact]
    public void HandleMessage_WithInvalidTraceparent_StillAcks()
    {
        var headers = new Dictionary<string, object>
        {
            ["traceparent"] = Encoding.UTF8.GetBytes("not-a-valid-traceparent")
        };
        var ea = MakeDelivery("{}", headers: headers);

        _sut.HandleMessage(_channelMock.Object, ea);

        _channelMock.Verify(c => c.BasicAck(It.IsAny<ulong>(), false), Times.Once);
    }
}
