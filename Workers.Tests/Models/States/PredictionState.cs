namespace Workers.Tests.Models.States;

public record PredictionState
{
    public Guid HashId { get; init; }

    public DateTime ChangeDate { get; init; }

    public bool IsInProcessing { get; init; }
}
