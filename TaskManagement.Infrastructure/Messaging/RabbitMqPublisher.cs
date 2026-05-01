using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace TaskManagement.Infrastructure.Messaging;

/// <summary>Publishes raw JSON payloads to the RabbitMQ task-events queue.</summary>
public sealed class RabbitMqPublisher : IMessagePublisher, IDisposable
{
    /// <summary>ActivitySource name used to register RabbitMQ publish spans with OpenTelemetry.</summary>
    public const string ActivitySourceName = "TaskManagement.Infrastructure";

    private static readonly ActivitySource _activitySource = new(ActivitySourceName);

    private const string Exchange = "task-events";
    private const string Queue = "task-events";
    private const string RoutingKey = "task-events";

    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    /// <summary>Creates the publisher and establishes a persistent RabbitMQ connection.</summary>
    /// <param name="config">Application configuration (reads RabbitMq:Host/User/Password).</param>
    /// <param name="logger">Logger instance.</param>
    public RabbitMqPublisher(IConfiguration config, ILogger<RabbitMqPublisher> logger)
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = config["RabbitMq:Host"] ?? "rabbitmq",
            UserName = config["RabbitMq:User"] ?? "guest",
            Password = config["RabbitMq:Password"] ?? "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        _channel.ExchangeDeclare(Exchange, ExchangeType.Direct, durable: true);
        _channel.QueueDeclare(Queue, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(Queue, Exchange, RoutingKey);
    }

    /// <summary>Publishes a JSON payload to the task-events queue.</summary>
    /// <param name="payload">JSON string to publish.</param>
    public Task PublishAsync(string payload)
    {
        using var activity = _activitySource.StartActivity("rabbitmq publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", Exchange);
        activity?.SetTag("messaging.destination_kind", "exchange");

        try
        {
            var body = Encoding.UTF8.GetBytes(payload);
            var props = _channel.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";

            // Inject W3C trace context so the consumer can link its span to this one
            if (activity != null && activity.IdFormat == ActivityIdFormat.W3C)
            {
                props.Headers = new Dictionary<string, object>
                {
                    ["traceparent"] = Encoding.UTF8.GetBytes(activity.Id!)
                };
                if (!string.IsNullOrEmpty(activity.TraceStateString))
                    props.Headers["tracestate"] = Encoding.UTF8.GetBytes(activity.TraceStateString);
            }

            _channel.BasicPublish(Exchange, RoutingKey, props, body);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to publish message to RabbitMQ");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
