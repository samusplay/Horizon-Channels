using System.Text.Json;
using Horizon.Channels.Common.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Horizon.Channels.Infrastructure.Messaging;

internal sealed class RabbitMqEventPublisher : IEventPublisher, IAsyncDisposable
{
    private const string Exchange = "horizon.events";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public RabbitMqEventPublisher(IOptions<RabbitMqOptions> options)
    {
        var o = options.Value;
        _factory = new ConnectionFactory
        {
            HostName = o.Host,
            Port = o.Port,
            UserName = o.User,
            Password = o.Password
        };
    }

    public async Task PublishAsync<TEvent>(string routingKey, Guid eventId, TEvent @event, CancellationToken ct)
    {
        var connection = await GetConnectionAsync(ct);

        // confirmaciones del publicador: el await no termina hasta que RabbitMQ diga "lo guardé"
        await using var channel = await connection.CreateChannelAsync(
            new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true),
            ct);

        // mismo exchange que declara Notifications (idempotente)
        await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent, // sobrevive a un reinicio de RabbitMQ
            MessageId = eventId.ToString()
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, Json);

        await channel.BasicPublishAsync(Exchange, routingKey, mandatory: false, basicProperties: properties, body: body, cancellationToken: ct);
    }

    // UNA conexión para toda la app, creada la primera vez que se publica
    private async Task<IConnection> GetConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            if (_connection is not null) await _connection.DisposeAsync();

            _connection = await _factory.CreateConnectionAsync(ct);
            return _connection;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null) await _connection.DisposeAsync();
        _lock.Dispose();
    }
}