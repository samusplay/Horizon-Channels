namespace Horizon.Channels.Infrastructure.Storage;

// la sección "AzureStorage" de la configuración, como clase tipada
public sealed class AzureStorageOptions
{
    public const string Section = "AzureStorage";

    public string ConnectionString { get; init; } = string.Empty;
    public string BannersContainer { get; init; } = "banners";
    public string RecordingsContainer { get; init; } = "recordings";
}