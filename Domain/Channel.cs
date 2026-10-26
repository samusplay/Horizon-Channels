namespace Horizon.Channels.Domain;

public enum ChannelStatus { PendingConfirmation, Active }

// lo que puede pasar al intentar confirmar
public enum ConfirmResult { Confirmed, Expired }

// lo que puede pasar al pedir un nuevo enlace de confirmación
public enum RenewResult { Renewed, AlreadyConfirmed, TooSoon }

public sealed class Channel
{
    // cuánto dura un enlace de confirmación
    public static readonly TimeSpan ConfirmationLifetime = TimeSpan.FromHours(24);

    // tiempo mínimo entre dos reenvíos (evita spam de correos)
    public static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);

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
            ConfirmationExpiresAt = nowUtc + ConfirmationLifetime,
            CreatedAt = nowUtc,
        };
    }

    // pasa el canal de "Pendiente" a "Activo" si el enlace sigue vigente
    public ConfirmResult Confirm(DateTime nowUtc)
    {
        if (ConfirmationExpiresAt is null || ConfirmationExpiresAt < nowUtc)
            return ConfirmResult.Expired;

        Status = ChannelStatus.Active;
        ConfirmedAt = nowUtc;
        ConfirmationTokenHash = null;   // el token sirve una sola vez
        ConfirmationExpiresAt = null;
        return ConfirmResult.Confirmed;
    }

    // reemplaza el token por uno nuevo (el anterior deja de servir)
    public RenewResult RenewConfirmation(string newTokenHash, DateTime nowUtc)
    {
        if (Status == ChannelStatus.Active)
            return RenewResult.AlreadyConfirmed;

        // el último envío fue cuando se generó el token actual: vencimiento menos su duración
        var lastSentAt = ConfirmationExpiresAt - ConfirmationLifetime;
        if (lastSentAt is not null && nowUtc - lastSentAt < ResendCooldown)
            return RenewResult.TooSoon;

        ConfirmationTokenHash = newTokenHash;
        ConfirmationExpiresAt = nowUtc + ConfirmationLifetime;
        return RenewResult.Renewed;
    }

    // ¿este usuario es el dueño del canal?
    public bool IsOwnedBy(Guid userId) => OwnerId == userId;

    // cambia el banner y devuelve el anterior, para poder borrarlo del almacenamiento
    public string? ChangeBanner(string newBannerUrl)
    {
        var previous = BannerUrl;
        BannerUrl = newBannerUrl;
        return previous;
    }

    // cambia los datos visibles del canal (el handle no se puede cambiar)
    public void UpdateProfile(string name, string? description)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }
}