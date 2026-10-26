namespace Horizon.Channels.Features.Channels.UploadBanner;

public sealed class UploadBannerValidator : AbstractValidator<UploadBannerCommand>
{
    private const long MaxBytes = 5 * 1024 * 1024; // 5 MB

    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];

    public UploadBannerValidator()
    {
        RuleFor(x => x.ContentType)
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("El banner debe ser una imagen JPG, PNG o WEBP.");

        RuleFor(x => x.Length)
            .GreaterThan(0).WithMessage("El archivo está vacío.")
            .LessThanOrEqualTo(MaxBytes).WithMessage("El banner no puede pesar más de 5 MB.");
    }
}