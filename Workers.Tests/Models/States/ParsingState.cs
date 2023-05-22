namespace Workers.Tests.Models.States;

public record ParsingState
{
    public string FileName { get; init; } = null!;

    public DateTime DataDate { get; init; }

    public bool IsInProcessing { get; init; }
}
