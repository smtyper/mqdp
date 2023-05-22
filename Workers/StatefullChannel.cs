using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Workers;

public abstract class StatefullChannel<TWorkItem> where TWorkItem : WorkItem<TWorkItem>
{
    private readonly Channel<TWorkItem> _channel;

    protected StatefullChannel() =>
        _channel = Channel.CreateUnbounded<TWorkItem>(
            new UnboundedChannelOptions { SingleWriter = false, SingleReader = false });

    public async Task AddAsync(TWorkItem workItem, CancellationToken cancellationToken)
    {
        var existedWorkItem = await GetWorkItemAsync(workItem, cancellationToken);

        if (existedWorkItem is null)
        {
            workItem = workItem with { IsInProcessing = true };

            await SetWorkItemAsync(workItem, cancellationToken);
            await _channel.Writer.WriteAsync(workItem, cancellationToken);

            return;
        }

        if (workItem.AreEqualByValue(existedWorkItem))
            return;

        workItem = workItem with { IsInProcessing = true };

        await SetWorkItemAsync(workItem, cancellationToken);

        if (!existedWorkItem!.IsInProcessing)
            await _channel.Writer.WriteAsync(workItem, cancellationToken);
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

    public abstract Task SetWorkItemAsync(TWorkItem workItem, CancellationToken cancellationToken);

    protected abstract Task<TWorkItem?> GetWorkItemAsync(TWorkItem workItem, CancellationToken cancellationToken);

    protected abstract Task<IReadOnlyCollection<TWorkItem>> GetWorkItemsInProcessingAsync(
        CancellationToken cancellationToken);
}

public abstract record WorkItem<T> where T : WorkItem<T>
{
    public bool IsInProcessing { get; set; }

    public abstract bool AreEqualByValue(T workItem);

    public abstract T WithMinimalValue();
}
