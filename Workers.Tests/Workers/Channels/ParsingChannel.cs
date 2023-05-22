using Workers.Tests.Databases;

namespace Workers.Tests.Workers.Channels;

public class ParsingChannel : StatefullChannel<ParsingWorkItem>
{
    private readonly TestDatabase _testDatabase;

    public ParsingChannel(TestDatabase testDatabase) => _testDatabase = testDatabase;

    public override async Task SetWorkItemAsync(ParsingWorkItem workItem, CancellationToken cancellationToken) =>
        await _testDatabase.SetParsingStateAsync(workItem.FileName, workItem.DataDate, workItem.IsInProcessing,
            cancellationToken);

    protected override async Task<ParsingWorkItem?> GetWorkItemAsync(ParsingWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var state = await _testDatabase.GetParsingStateAsync(workItem.FileName, cancellationToken);

        var result = state is null ?
            null :
            new ParsingWorkItem(state.FileName, state.DataDate) { IsInProcessing = workItem.IsInProcessing };

        return result;
    }

    protected override async Task<IReadOnlyCollection<ParsingWorkItem>> GetWorkItemsInProcessingAsync(
        CancellationToken cancellationToken) =>
        (await _testDatabase.GetParsingStateInProcessingAsync(cancellationToken))
        .Select(state => new ParsingWorkItem(state.FileName, state.DataDate) { IsInProcessing = state.IsInProcessing })
        .ToArray();
}
