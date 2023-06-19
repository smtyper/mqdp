using Reputation.Data.Processing.Models;

namespace Workers.Tests.Models.Rosstat;

public class BankruptcyPrediction : HashableObject
{
    [IncludedToHash]
    public Guid ParentHashId { get; init; }

    [IncludedToHash]
    public BankruptcyPredictionModel Model { get; init; }

    public decimal Score { get; init; }

    public BankruptcyProbability Probability { get; init; }

    public DateTime ChangeDate { get; set; }
}
