using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Workers;

public abstract class EternalWorker<TWorkItem, TWorkerChannel, TOptions>  :
    BackgroundService, IConfigurableHostedService
    where TWorkItem : WorkItem
    where TOptions : WorkerSettings, new()
    where TWorkerChannel : StatefullChannel<TWorkItem>
{
    private readonly string _workerName;

    protected TWorkerChannel Channel { get; }

    protected TOptions Settings { get; }

    protected ILogger Logger { get; }

    protected EternalWorker(TWorkerChannel channel, IOptions<TOptions> options, ILogger logger)
    {
        Channel = channel ?? throw new ArgumentNullException(nameof(channel));
        Settings = options.Value ?? throw new ArgumentNullException(nameof(options));
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _workerName = GetType().Name.TrimEnd("Worker");
    }

    public sealed override async Task StartAsync(CancellationToken cancellationToken)
    {
        using var workerScope = Logger.BeginScope(new Dictionary<string, object> { ["WorkerName"] = _workerName });

        try
        {
            Logger.LogInformation("{WorkerName} initializing");

            await InitializeAsync().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }

        await base.StartAsync(cancellationToken).ConfigureAwait(false);
    }

    public sealed override Task StopAsync(CancellationToken cancellationToken)
    {
        using var workerScope = Logger.BeginScope(new Dictionary<string, object> { ["WorkerName"] = _workerName });

        return base.StopAsync(cancellationToken);
    }

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
            try
            {
                using var workerSessionScope =
                    Logger.BeginScope(new Dictionary<string, object> { ["WorkerSessionId"] = Guid.NewGuid() });

                using (Logger.BeginScope(new Dictionary<string, object>
                       {
                           [nameof(Settings.ConcurrentTaskCount)] = Settings.ConcurrentTaskCount
                       }))
                    Logger.LogInformation("{WorkerName} running. Concurrent task count: {ConcurrentTaskCount}");

                var workerTasks = Enumerable.Range(0, Settings.ConcurrentTaskCount).Select(i =>
                    Task.Run(async () => await WorkAsync(i, stoppingToken).ConfigureAwait(false),
                        stoppingToken));
                var tasks = Task.WhenAll(workerTasks);

                try
                {
                    await tasks.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }

                Logger.LogInformation(stoppingToken.IsCancellationRequested ?
                    "{WorkerName} stopped" :
                    "{WorkerName} finished");

                try
                {
                    await Task.Delay(Settings.IdleTime, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "{WorkerName} failed");
            }
    }

    private async Task WorkAsync(int concurrentTaskIndex, CancellationToken stoppingToken)
    {
        using var concurrentTaskScope =
            Logger.BeginScope(new Dictionary<string, object> { ["ConcurrentTaskIndex"] = concurrentTaskIndex });

        try
        {
            var stopwatch = new Stopwatch();

            await foreach (var workItem in Channel.GetWorkItemsAsync(stoppingToken).ConfigureAwait(false))
            {
                stoppingToken.ThrowIfCancellationRequested();

                using var workItemScope =
                    Logger.BeginScope(new Dictionary<string, object?> { ["WorkItemKey"] = workItem?.ToString() });

                try
                {
                    stopwatch.Restart();

                    await ProcessWorkItemAsync(workItem!, stoppingToken).ConfigureAwait(false);

                    using (Logger.BeginScope(new Dictionary<string, object>
                           {
                               [nameof(stopwatch.ElapsedMilliseconds)] = stopwatch.ElapsedMilliseconds,
                               ["MetricName"] = nameof(ProcessWorkItemAsync)
                           }))
                        Logger.LogInformation("{WorkerName} processed {WorkItemKey} in {ElapsedMilliseconds} ms");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                }
                catch (Exception ex)
                {
                    using (Logger.BeginScope(new Dictionary<string, object>
                           {
                               [nameof(stopwatch.ElapsedMilliseconds)] = stopwatch.ElapsedMilliseconds,
                               ["MetricName"] = nameof(ProcessWorkItemAsync)
                           }))
                        Logger.LogError(ex, "{WorkerName} failed {WorkItemKey} in {ElapsedMilliseconds} ms");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{WorkerName} worker task {ConcurrentTaskIndex} failed");
        }
    }

    protected virtual Task InitializeAsync() => Task.CompletedTask;

    protected abstract IAsyncEnumerable<TWorkItem> GetWorkItemsAsync(CancellationToken cancellationToken);

    protected abstract Task ProcessWorkItemAsync(TWorkItem workItem, CancellationToken cancellationToken);
}
