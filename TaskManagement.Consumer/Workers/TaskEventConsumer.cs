using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace TaskManagement.Consumer.Workers;

/// <summary>Background service that reads task events from RabbitMQ and logs them.</summary>
public sealed class TaskEventConsumer : BackgroundService
{
    /// <summary>ActivitySource name used to register consumer spans with OpenTelemetry.</summary>
    public const string ActivitySourceName = "TaskManagement.Consumer";

    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    private const string Exchange = "task-events";
    private const string Queue = "task-events";
    private const string RoutingKey = "task-events";

    private readonly ILogger<TaskEventConsumer> _logger;
    private readonly IConfiguration _config;
    private IConnection? _connection;
    private IModel? _channel;

    public TaskEventConsumer(ILogger<TaskEventConsumer> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Retry until RabbitMQ is ready (relevant on Docker startup)
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                Connect();
                _logger.LogInformation("Connected to RabbitMQ, waiting for events...");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "RabbitMQ not ready, retrying in 5 s...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        var consumer = new EventingBasicConsumer(_channel!);
        consumer.Received += (_, ea) => HandleMessage(_channel!, ea);

        _channel!.BasicConsume(Queue, autoAck: false, consumer: consumer);

        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }

    // internal for unit testing
    internal void HandleMessage(IModel channel, BasicDeliverEventArgs ea)
    {
        ActivityContext parentContext = default;
        if (ea.BasicProperties?.Headers != null)
        {
            if (ea.BasicProperties.Headers.TryGetValue("traceparent", out var tpRaw) && tpRaw is byte[] tpBytes)
            {
                var traceparent = Encoding.UTF8.GetString(tpBytes);
                string? tracestate = null;
                if (ea.BasicProperties.Headers.TryGetValue("tracestate", out var tsRaw) && tsRaw is byte[] tsBytes)
                    tracestate = Encoding.UTF8.GetString(tsBytes);
                ActivityContext.TryParse(traceparent, tracestate, isRemote: true, out parentContext);
            }
        }

        using var activity = _activitySource.StartActivity("rabbitmq receive", ActivityKind.Consumer, parentContext);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", Queue);
        activity?.SetTag("messaging.destination_kind", "queue");

        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        _logger.LogInformation("Received task event: {Json}", json);
        channel.BasicAck(ea.DeliveryTag, multiple: false);
    }

    private void Connect()
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMq:Host"] ?? "rabbitmq",
            UserName = _config["RabbitMq:User"] ?? "guest",
            Password = _config["RabbitMq:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(Exchange, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(Queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(Queue, Exchange, RoutingKey);
    }

    /// <inheritdoc/>
    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
