namespace Workers.Tests.Models.States;

public record DownloadingState
{
    public string FileName { get; init; } = null!;

    public DateTime DataDate { get; init; }
}
