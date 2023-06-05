using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using Workers.Tests.Databases;
using Workers.Tests.HttpClients;
using Workers.Tests.Models.Raw;
using Workers.Tests.Workers.Channels;

namespace Workers.Tests.Workers;

internal class DownloadingWorker : Worker<DownloadingWorkItem, DownloadingSettings>
{
    private readonly RosstatGovClient _rosstatGovClient;
    private readonly TestDatabase _testDatabase;
    private readonly ParsingChannel _parsingChannel;

    public DownloadingWorker(IOptions<DownloadingSettings> options, ILogger<DownloadingWorker> logger,
        ParsingChannel parsingChannel, RosstatGovClient rosstatGovClient, TestDatabase testDatabase) :
        base(options, logger)
    {
        _rosstatGovClient = rosstatGovClient;
        _testDatabase = testDatabase;
        _parsingChannel = parsingChannel;
    }

    protected override async IAsyncEnumerable<DownloadingWorkItem> GetWorkItemsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var sourceFiles = _rosstatGovClient
            .GetFinancialRegistriesAsync()
            .SelectMany(registry => _rosstatGovClient
                .GetFilesFromRegistryAsync(registry.RegistryId, registry.Url)
                .Distinct())
            .Take(Settings.FilesCount)
            .ToEnumerable();
        var states = await _testDatabase.GetDownloadingStatesAsync(cancellationToken);

        var unprocessedFiles = sourceFiles.Except(states.Select(state => (state.RegistryId, state.FileName)));

        foreach (var (registryId, fileName) in unprocessedFiles)
            yield return new DownloadingWorkItem(registryId, fileName);
    }

    protected override async Task ProcessWorkItemAsync(DownloadingWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var (registryId, fileName) = workItem;

        var datasetFile = await DownloadFileAsync(registryId, fileName, cancellationToken);

        datasetFile = await _testDatabase.InsertRawDatasetFileAsync(datasetFile, cancellationToken);
        await AddDatasetFileToParsingChannelAsync(datasetFile, cancellationToken);

        await _testDatabase.InsertDownloadingStateAsync(registryId, fileName, cancellationToken);
    }

    private async ValueTask<DatasetFile> DownloadFileAsync(string registryId, string fileName,
        CancellationToken cancellationToken)
    {
        var url = $"opendata/{registryId}/{fileName}";
        var datasetFile = new DatasetFile { RegistryId = registryId, FileName = fileName };

        var temporaryFolder = Settings.TemporaryFolderPath;
        var temporaryPath = Path.Combine(temporaryFolder, $"{Guid.NewGuid()}.{fileName}.download");

        var destinationFolder = Path.Combine(Settings.DestinationFolderPath, registryId);
        var destinationPath = Path.Combine(destinationFolder, fileName);

        if (File.Exists(destinationPath))
            return datasetFile;

        Directory.CreateDirectory(temporaryFolder);

        await using (var stream = await _rosstatGovClient.GetFileStreamAsync(url, cancellationToken))
        {
            await using var fileStream = File.Create(temporaryPath);
            await stream.CopyToAsync(fileStream, cancellationToken);
        }

        Directory.CreateDirectory(destinationFolder);
        File.Move(temporaryPath, destinationPath);

        return datasetFile;
    }

    private async ValueTask AddDatasetFileToParsingChannelAsync(DatasetFile datasetFile,
        CancellationToken cancellationToken)
    {
        var parsingWorkItem = new ParsingWorkItem(datasetFile.RegistryId, datasetFile.FileName, datasetFile.ChangeDate);

        await _parsingChannel.AddAsync(parsingWorkItem, cancellationToken);
    }
}

internal record DownloadingWorkItem(string RegistryId, string FileName)
{
    public override string ToString() => $"{RegistryId} {FileName}";
}

internal class DownloadingSettings : WorkerSettings
{
    private string _destinationFolderPath = null!;

    [Required]
    public string DestinationFolderPath
    {
        get => _destinationFolderPath;
        set => _destinationFolderPath = string.IsNullOrEmpty(value) ? value : Path.Combine(value, "rosstat");
    }

    [Required]
    public string TemporaryFolderPath { get; set; } = null!;

    [Required]
    public bool SyncDatabaseWithStorage { get; set; }

    public int FilesCount { get; init; }
}
