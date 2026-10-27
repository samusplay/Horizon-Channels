namespace Horizon.Channels.Common.Messaging.Events;

// Contrato publicado hacia Notifications (debe coincidir con su copia)
public sealed record ChannelConfirmationRequested(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string Email,
    string ChannelName,
    string ConfirmationUrl)
{
    public const string RoutingKey = "channels.confirmation-requested";
}