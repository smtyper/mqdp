using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using Workers.Tests.Databases;
using Workers.Tests.Models.Rosstat;
using Workers.Tests.Workers.Channels;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Workers.Tests.Workers;

internal class ParsingWorker : EternalWorker<ParsingWorkItem, ParsingChannel, ParsingSettings>
{
    private readonly TestDatabase _testDatabase;
    private readonly PredictionChannel _predictionChannel;

    public ParsingWorker(TestDatabase testDatabase, ParsingChannel parsingChannel, IOptions<ParsingSettings> options,
        ILogger<ParsingWorker> logger, PredictionChannel predictionChannel) : base(parsingChannel, options, logger)
    {
        _testDatabase = testDatabase;
        _predictionChannel = predictionChannel;
    }

    protected override async Task ProcessWorkItemAsync(ParsingWorkItem workItem,
        CancellationToken cancellationToken)
    {
        var (registryId, fileName, _) = workItem;
        var datasetDate = GetDatasetDate(fileName);
        var period = GetDatasetPeriod(registryId);

        var filePath = Path.Combine(Settings.SourceFolderPath, registryId, fileName);
        var reports = GetRosstatReportsAsync(filePath, period, datasetDate).Take(Settings.ReportsCount);

        await foreach (var report in reports.WithCancellation(cancellationToken))
        {
            var reportWithChangeDate = await _testDatabase.InsertRosstatReportAsync(report, cancellationToken);
            await AddReportToPredictionChannelAsync(reportWithChangeDate, cancellationToken);
        }
    }

    private static async IAsyncEnumerable<Report> GetRosstatReportsAsync(string filePath, int period,
        DateTime datesetDate)
    {
        await using var archiveStream = File.OpenRead(filePath);
        using var archive = new ZipArchive(archiveStream);
        await using var fileStream = archive.Entries.Single().Open();

        using var streamReader = new StreamReader(fileStream, Encoding.GetEncoding(1251));
        using var csvReader = new CsvReader(streamReader,
            new CsvConfiguration(CultureInfo.GetCultureInfo("ru-RU"))
            {
                Mode = CsvMode.NoEscape
            });
        var classMap = csvReader.Context.RegisterClassMap<ReportMap>();
        classMap.Map(report => report.Period).Constant(period);
        classMap.Map(report => report.DataDate).Constant(datesetDate);

        await foreach (var report in csvReader.GetRecordsAsync<Report>())
        {
            report.Values = JsonSerializer.Serialize(report.ValuesDictionary);

            yield return report.WithHashId()!;
        }
    }

    private static DateTime GetDatasetDate(string fileName) =>
        DateTime.ParseExact(fileName.Split('-')[1], "yyyyMMdd", null);

    private static int GetDatasetPeriod(string registryId) => int.Parse(registryId.Split("bdboo").Last());

    private async ValueTask AddReportToPredictionChannelAsync(Report report,
        CancellationToken cancellationToken)
    {
        var predictionWorkItem = new PredictionWorkItem(report.HashId!.Value, report.ChangeDate);

        await _predictionChannel.AddAsync(predictionWorkItem, cancellationToken);
    }
}

internal record ParsingWorkItem(string RegistryId, string FileName, DateTime? ChangeDate) : WorkItem<ParsingWorkItem>
{
    public override bool AreEqualByValue(ParsingWorkItem workItem) => ChangeDate == workItem.ChangeDate;

    public override ParsingWorkItem WithMinimalValue() => this with { ChangeDate = DateTime.MinValue };

    public override string ToString() => $"{RegistryId} {FileName}";
}

internal class ParsingSettings : WorkerSettings
{
    private string _sourceFolderPath = null!;

    [Required]
    public string SourceFolderPath
    {
        get => _sourceFolderPath;
        set => _sourceFolderPath = string.IsNullOrEmpty(value) ? value : Path.Combine(value, "rosstat");
    }

    public int ReportsCount { get; init; }
}
