using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace ClaimsProcessor.Services.Messaging;

public class RabbitMQPublisher : IMessagePublisher, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private readonly string _exchangeName = "claims.events";

    public RabbitMQPublisher(ILogger<RabbitMQPublisher> logger, IConfiguration configuration)
    {
        _logger = logger;

        try
        {
            var rabbitmqUrl = configuration.GetConnectionString("RabbitMQ") ?? 
                              Environment.GetEnvironmentVariable("RABBITMQ_URL") ?? 
                              "amqp://claims:claims123@localhost:5672/";

            var factory = new ConnectionFactory { Uri = new Uri(rabbitmqUrl) };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Declare the exchange
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            _logger.LogInformation("RabbitMQ publisher initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ publisher");
            throw;
        }
    }

    public async Task PublishClaimEvent(string eventType, int claimId, int patientId)
    {
        var claimEvent = new
        {
            EventType = eventType,
            ClaimId = claimId,
            PatientId = patientId,
            Timestamp = DateTime.UtcNow
        };

        await PublishMessage(eventType, claimEvent);
    }

    public async Task PublishMessage<T>(string routingKey, T message) where T : class
    {
        try
        {
            var jsonMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var body = Encoding.UTF8.GetBytes(jsonMessage);

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: routingKey,
                basicProperties: properties,
                body: body);

            _logger.LogDebug("Published message to RabbitMQ: {RoutingKey}", routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to RabbitMQ: {RoutingKey}", routingKey);
            // Don't throw here to avoid breaking the main workflow
        }

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
            _channel?.Close();
            _channel?.Dispose();
            _connection?.Close();
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing RabbitMQ connection");
        }
    }
}