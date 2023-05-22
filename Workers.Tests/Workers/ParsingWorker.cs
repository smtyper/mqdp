using Microsoft.Extensions.Options;
using Workers.Tests.Databases;
using Workers.Tests.Models.Calculations;
using Workers.Tests.Workers.Channels;

namespace Workers.Tests.Workers;

public class ParsingWorker : EternalWorker<ParsingWorkItem, ParsingChannel, ParsingSettings>
{
    private readonly TestDatabase _testDatabase;

    public ParsingWorker(ParsingChannel channel, IOptions<ParsingSettings> options, ILogger<ParsingWorker> logger,
        TestDatabase testDatabase) : base(channel, options, logger) => _testDatabase = testDatabase;

    protected override async Task ProcessWorkItemAsync(ParsingWorkItem workItem, CancellationToken cancellationToken)
    {
        var (fileName, _) = workItem;

        var fileSum = new FileSum { FileName = fileName, Sum = (decimal)Random.Shared.NextDouble() };

        await _testDatabase.InsertFileSumAsync(fileSum, cancellationToken);
    }
}

public record ParsingWorkItem(string FileName, DateTime DataDate) : WorkItem<ParsingWorkItem>
{
    public override bool AreEqualByValue(ParsingWorkItem workItem) => FileName == workItem.FileName &&
                                                                      DataDate == workItem.DataDate;

    public override ParsingWorkItem WithMinimalValue() => this with { DataDate = DateTime.MinValue };

    public override string ToString() => $"{FileName}";
}

public class ParsingSettings : WorkerSettings
{
}
