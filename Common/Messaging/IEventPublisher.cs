namespace Horizon.Channels.Common.Messaging;

// Puerto: "avisa al resto del sistema que pasó algo"
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(string routingKey, Guid eventId, TEvent @event, CancellationToken ct);
}