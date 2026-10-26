namespace Horizon.Channels.Features.Channels.ConfirmChannel;

public sealed class ConfirmChannelValidator : AbstractValidator<ConfirmChannelCommand>
{
    public ConfirmChannelValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Falta el token de confirmación.")
            .Matches("^[A-Za-z0-9_-]{43}$").WithMessage("El enlace de confirmación no es válido.");
    }
}