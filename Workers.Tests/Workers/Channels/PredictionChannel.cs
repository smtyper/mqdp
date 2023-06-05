using Workers.Tests.Databases;

namespace Workers.Tests.Workers.Channels;

internal class PredictionChannel : StatefullChannel<PredictionWorkItem>
{
    private readonly TestDatabase _testDatabase;

    public PredictionChannel(TestDatabase testDatabase) => _testDatabase = testDatabase;

    public override async Task SetWorkItemAsync(PredictionWorkItem workItem, CancellationToken cancellationToken)
    {
        var (hashId, changeDate) = workItem;

        await _testDatabase.SetPredictionStateAsync(hashId, changeDate, workItem.IsInProcessing, cancellationToken);
    }

    protected override async Task<PredictionWorkItem?> GetWorkItemAsync(PredictionWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var (hashId, _) = workItem;

        var state = await _testDatabase.GetPredictionStateAsync(hashId, cancellationToken);
        var existedWorkItem = state is null ?
            null :
            new PredictionWorkItem(state.HashId, state.ChangeDate)
            {
                IsInProcessing = state.IsInProcessing
            };

        return existedWorkItem;
    }

    protected override async Task<IReadOnlyCollection<PredictionWorkItem>> GetWorkItemsInProcessingAsync(
        CancellationToken cancellationToken) =>
        (await _testDatabase.GetPredictionStatesInProcessingAsync(cancellationToken))
        .Select(state => new PredictionWorkItem(state.HashId, state.ChangeDate)
        {
            IsInProcessing = state.IsInProcessing
        })
        .ToArray();
}
