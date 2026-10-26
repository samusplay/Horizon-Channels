namespace Horizon.Channels.Features.Channels.UpdateChannel;

public sealed class UpdateChannelValidator : AbstractValidator<UpdateChannelCommand>
{
    public UpdateChannelValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre admite máximo 50 caracteres.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("La descripción admite máximo 500 caracteres.");
    }
}