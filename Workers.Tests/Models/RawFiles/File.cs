namespace Workers.Tests.Models.RawFiles;

public record File
{
    public string FileName { get; init; } = null!;

    public DateTime DataDate { get; init; }
}
