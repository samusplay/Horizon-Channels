namespace Horizon.Channels.Features.Channels.ListChannels;

public sealed class ListChannelsValidator : AbstractValidator<ListChannelsQuery>
{
    public ListChannelsValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("La página debe ser 1 o mayor.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("El tamaño de página debe estar entre 1 y 50.");
    }
}