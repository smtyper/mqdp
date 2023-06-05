namespace Workers.Tests.Models.Raw;

internal record DatasetFile
{
    public required string RegistryId { get; init; }

    public required string FileName { get; init; }

    public DateTime ChangeDate { get; init; }
}
