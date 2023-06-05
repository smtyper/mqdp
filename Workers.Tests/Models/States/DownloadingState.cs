namespace Workers.Tests.Models.States;

internal record DownloadingState
{
    public required string RegistryId { get; init; }

    public required string FileName { get; init; }
}
