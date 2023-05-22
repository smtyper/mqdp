namespace Workers.Tests.Models.Calculations;

public record FileSum
{
    public required string FileName { get; init; }

    public decimal Sum { get; init; }

    public DateTime ChangeDate { get; init; }
}
