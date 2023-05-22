using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using MoreLinq.Extensions;
using Workers.Tests.Databases;
using Workers.Tests.Models.RawFiles;
using Workers.Tests.Models.States;
using Workers.Tests.Workers.Channels;
using File = Workers.Tests.Models.RawFiles.File;

namespace Workers.Tests.Workers;

public class DownloadingWorker : Worker<DownloadingWorkItem, DownloadingSettings>
{
    private readonly TestDatabase _testDatabase;

    private readonly ParsingChannel _parsingChannel;

    public DownloadingWorker(IOptions<DownloadingSettings> options, ILogger<DownloadingWorker> logger,
        TestDatabase testDatabase, ParsingChannel parsingChannel) : base(options, logger)
    {
        _testDatabase = testDatabase;
        _parsingChannel = parsingChannel;
    }

    protected override async IAsyncEnumerable<DownloadingWorkItem> GetWorkItemsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sourceFiles = GetSourceFilesAsync();
        var states = await _testDatabase.GetDownloadingStatesAsync(cancellationToken);

        var unprocessedFiles = sourceFiles
            .Select(pair => new DownloadingState { FileName = pair.FileName, DataDate = pair.DataDate })
            .FullJoin(states,
                state => state.FileName,
                source => (source, state: null)!,
                state => (source: null, state)!,
                (source, state) => (source, state))
            .Where(pair => pair.source?.DataDate != pair.state?.DataDate)
            .Select(pair => (pair.source?.FileName ?? pair.state.FileName, ChangeDate: pair.source?.DataDate));

        foreach (var (fileName, dataDate) in unprocessedFiles)
            yield return new DownloadingWorkItem(fileName, dataDate);
    }

    protected override async Task ProcessWorkItemAsync(DownloadingWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var (fileName, dataDate) = workItem;

        var file = await DownloadFileAsync(fileName, dataDate, cancellationToken);

        await _testDatabase.SetDownloadingStateAsync(fileName, DateTime.MinValue, cancellationToken);

        if (file is not null)
        {
            await _testDatabase.InsertRawFilesFileAsync(file, cancellationToken);
            await _parsingChannel.AddAsync(new ParsingWorkItem(fileName, dataDate!.Value), cancellationToken);
        }

        await _testDatabase.SetDownloadingStateAsync(fileName, dataDate, cancellationToken);
    }

    private async ValueTask<File?> DownloadFileAsync(string fileName, DateTime? dataDate,
        CancellationToken cancellationToken) => dataDate is null ?
        null :
        new File { FileName = fileName, DataDate = dataDate.Value };

    private static IReadOnlyCollection<(string FileName, DateTime DataDate)> GetSourceFilesAsync()
    {
        var random = new Random(DateTime.UtcNow.Microsecond);

        var skipCount = random.Next(10);

        var files = new[]
            {
                "oleg.adm", "oleg.german", "oleg", "oleg", "olegblud", "olegserebry", "olegsxm", "olejan1991",
                "oleksii.kom", "olelishna", "oleynik.mik", "olga.tyulik", "olkhovskiy", "omg.anomaly", "omuftiev",
                "onigae", "onokhova", "ooayaoo", "openyshev.r", "opisania", "or56", "orakool", "order",
                "originalzed", "orrrrb", "os.salenko", "os_alex", "osdmit", "osdmit", "osi-v", "ostashevdv",
                "os-unlimite"
            }
            .Skip(skipCount)
            .Select(fileName => (fileName, DateTime.UtcNow.AddDays(-1 * random.Next(7))))
            .ToArray();

        return files;
    }
}

public record DownloadingWorkItem(string FileName, DateTime? DataDate)
{
    public override string ToString() => $"{FileName}";
}

public class DownloadingSettings : WorkerSettings
{
}
