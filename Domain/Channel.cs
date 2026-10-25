namespace Horizon.Channels.Domain;

public enum ChannelStatus { PendingConfirmation, Active }

public sealed class Channel
{
    public Guid Id { get; private set; }
    public Guid OwnerId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Handle { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? BannerUrl { get; private set; }
    public ChannelStatus Status { get; private set; }
    public string? ConfirmationTokenHash { get; private set; }
    public DateTime? ConfirmationExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }

    private Channel() { } // para EF Core

    public static Channel Create(Guid ownerId, string name, string handle,
        string? description, string confirmationTokenHash, DateTime nowUtc)
    {
        return new Channel
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = name.Trim(),
            Handle = handle.Trim().ToLowerInvariant(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Status = ChannelStatus.PendingConfirmation,
            ConfirmationTokenHash = confirmationTokenHash,
            ConfirmationExpiresAt = nowUtc.AddHours(24),
            CreatedAt = nowUtc,
        };
    }
}