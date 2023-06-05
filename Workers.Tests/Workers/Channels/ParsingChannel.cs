using Workers.Tests.Databases;

namespace Workers.Tests.Workers.Channels;

internal class ParsingChannel : StatefullChannel<ParsingWorkItem>
{
    private readonly TestDatabase _testDatabase;

    public ParsingChannel(TestDatabase testDatabase) => _testDatabase = testDatabase;

    public override async Task SetWorkItemAsync(ParsingWorkItem workItem, CancellationToken cancellationToken)
    {
        var (registryId, fileName, changeDate) = workItem;

        await _testDatabase.SetParsingStateAsync(registryId, fileName, changeDate, workItem.IsInProcessing,
            cancellationToken);
    }

    protected override async Task<ParsingWorkItem?> GetWorkItemAsync(ParsingWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var (registryId, fileName, _) = workItem;

        var state = await _testDatabase.GetParsingStateAsync(registryId, fileName, cancellationToken);
        var existedWorkItem = state is null ?
            null :
            new ParsingWorkItem(state.RegistryId, state.FileName, state.ChangeDate)
            {
                IsInProcessing = state.IsInProcessing
            };

        return existedWorkItem;
    }

    protected override async Task<IReadOnlyCollection<ParsingWorkItem>> GetWorkItemsInProcessingAsync(
        CancellationToken cancellationToken) =>
        (await _testDatabase.GetParsingStatesInProcessingAsync(cancellationToken))
        .Select(state => new ParsingWorkItem(state.RegistryId, state.FileName, state.ChangeDate)
        {
            IsInProcessing = state.IsInProcessing
        })
        .ToArray();
}
