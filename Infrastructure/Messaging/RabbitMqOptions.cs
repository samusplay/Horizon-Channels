using System.ComponentModel.DataAnnotations;

namespace Horizon.Channels.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string Section = "RabbitMq";

    [Required] public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 5672;
    [Required] public string User { get; init; } = string.Empty;
    [Required] public string Password { get; init; } = string.Empty;
}