namespace Horizon.Channels.Features.Channels.CreateChannel;

public sealed class CreateChannelValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelValidator(ChannelsDbContext db)
    {
        RuleFor(x => x.OwnerId)
            .MustAsync(async (ownerId, ct) => !await db.Channels.AnyAsync(c => c.OwnerId == ownerId, ct))
            .WithMessage("Ya tienes un canal.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.Handle)
            .NotEmpty()
            .Matches("^[a-z0-9_-]{3,30}$")
            .WithMessage("El handle debe tener entre 3 y 30 caracteres: minúsculas, números, guion o guion bajo.")
            .MustAsync(async (handle, ct) => !await db.Channels.AnyAsync(c => c.Handle == handle, ct))
            .WithMessage("Ese handle ya está en uso.");

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}