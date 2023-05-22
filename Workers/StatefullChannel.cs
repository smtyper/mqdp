using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Workers;

public abstract class StatefullChannel<TWorkItem> where TWorkItem : WorkItem
{
    private readonly Channel<TWorkItem> _channel;

    protected StatefullChannel() =>
        _channel = Channel.CreateUnbounded<TWorkItem>(
            new UnboundedChannelOptions { SingleWriter = false, SingleReader = false });

    public async Task Add(TWorkItem workItem, CancellationToken cancellationToken)
    {
        var existedWorkItem = await GetWorkItemAsync(workItem, cancellationToken);

        if (!workItem.AreFullEqual(existedWorkItem))
        {
            var workItemInProcessing = workItem with { IsInProcessing = true };

            if (existedWorkItem is null)
            {
                await InsertWorkItemAsync(workItemInProcessing, cancellationToken);
                await _channel.Writer.WriteAsync(workItemInProcessing, cancellationToken);
            }
            else
            {
                await UpdateWorkItemAsync(workItemInProcessing, cancellationToken);

                if (!existedWorkItem.IsInProcessing)
                    await _channel.Writer.WriteAsync(workItem, cancellationToken);
            }
        }
    }

    public async IAsyncEnumerable<TWorkItem> GetWorkItemsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var workItem in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            var workItemFromStorage = await GetWorkItemAsync(workItem, cancellationToken);

            if (workItemFromStorage is null)
                throw new NullReferenceException(nameof(workItemFromStorage));

            yield return workItemFromStorage;
        }
    }

    public async Task SyncChannelWithStorageAsync(CancellationToken cancellationToken)
    {
        var workItemsInProcessing = await GetWorkItemsInProcessingAsync(cancellationToken);

        foreach (var workItem in workItemsInProcessing)
            await _channel.Writer.WriteAsync(workItem, cancellationToken);
    }

    protected abstract Task InsertWorkItemAsync(TWorkItem workItem, CancellationToken cancellationToken);

    protected abstract Task UpdateWorkItemAsync(TWorkItem workItem, CancellationToken cancellationToken);

    protected abstract Task<TWorkItem?> GetWorkItemAsync(TWorkItem workItem, CancellationToken cancellationToken);

    protected abstract Task<IReadOnlyCollection<TWorkItem>> GetWorkItemsInProcessingAsync(
        CancellationToken cancellationToken);
}

public abstract record WorkItem
{
    public bool IsInProcessing { get; set; }

    public abstract bool AreFullEqual(WorkItem? workItem);
}
