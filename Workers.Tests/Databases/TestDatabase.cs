using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using Microsoft.Extensions.Options;

namespace Workers.Tests.Databases;

internal class TestDatabase
{
    private readonly TestDatabaseSettings _settings;

    public TestDatabase(IOptions<TestDatabaseSettings> options) => _settings = options.Value;

    public async ValueTask<IReadOnlyCollection<Models.States.DownloadingState>> GetDownloadingStatesAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var states = await connection.GetTable<Models.States.DownloadingState>().ToArrayAsync(token: cancellationToken);

        return states;
    }

    public async ValueTask InsertDownloadingStateAsync(string registryId, string fileName,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var state = new Models.States.DownloadingState { RegistryId = registryId, FileName = fileName };

        if (!await connection.GetTable<Models.States.DownloadingState>().ContainsAsync(state, token: cancellationToken))
            await connection.InsertAsync(state, token: cancellationToken);
    }

    public async ValueTask<Models.Raw.DatasetFile> InsertRawDatasetFileAsync(Models.Raw.DatasetFile datasetFile,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var exitedFile = await connection
            .GetTable<Models.Raw.DatasetFile>()
            .SingleOrDefaultAsync(file => file.RegistryId == datasetFile.RegistryId &&
                                          file.FileName == datasetFile.FileName, token: cancellationToken);

        if (exitedFile is not null)
            return exitedFile;

        datasetFile = datasetFile with { ChangeDate = DateTime.UtcNow };

        await connection.InsertAsync(datasetFile, token: cancellationToken);

        return datasetFile;

    }

    public async ValueTask SetParsingStateAsync(string registryId, string fileName, DateTime? changeDate,
        bool isInProcessing, CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var state = new Models.States.ParsingState
        {
            RegistryId = registryId,
            FileName = fileName,
            IsInProcessing = isInProcessing
        };

        if (changeDate is null)
            await connection.DeleteAsync(state, token: cancellationToken);
        else if (await connection.GetTable<Models.States.ParsingState>().ContainsAsync(state, token: cancellationToken))
            await connection.UpdateAsync(state, token: cancellationToken);
        else
            await connection.InsertAsync(state, token: cancellationToken);
    }

    public async ValueTask<Models.States.ParsingState?> GetParsingStateAsync(string registryId, string fileName,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var state = await connection
            .GetTable<Models.States.ParsingState>()
            .SingleOrDefaultAsync(state => state.RegistryId == registryId && state.FileName == fileName,
                token: cancellationToken);

        return state;
    }

    public async ValueTask<Models.Rosstat.Report> InsertRosstatReportAsync(Models.Rosstat.Report report,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var existedReport = await connection
            .GetTable<Models.Rosstat.Report>()
            .SingleOrDefaultAsync(existedReport => existedReport.HashId == report.HashId, token: cancellationToken);

        if (existedReport is not null)
            return existedReport;

        report.ChangeDate = DateTime.UtcNow;

        await connection.InsertAsync(report, token: cancellationToken);

        return report;
    }

    public async ValueTask<IReadOnlyCollection<Models.States.ParsingState>> GetParsingStatesInProcessingAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var states = await connection
            .GetTable<Models.States.ParsingState>()
            .Where(state => state.IsInProcessing)
            .ToArrayAsync(token: cancellationToken);

        return states;
    }

    public async ValueTask SetPredictionStateAsync(Guid hashId, DateTime? changeDate, bool isInProcessing,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var state = new Models.States.PredictionState
        {
            HashId = hashId,
            IsInProcessing = isInProcessing
        };

        if (changeDate is null)
            await connection.DeleteAsync(state, token: cancellationToken);
        else
        {
            state = state with { ChangeDate = changeDate.Value };

            if (await connection
                    .GetTable<Models.States.PredictionState>()
                    .ContainsAsync(state, token: cancellationToken))
                await connection.UpdateAsync(state, token: cancellationToken);
            else
                await connection.InsertAsync(state, token: cancellationToken);
        }

    }

    public async ValueTask<Models.States.PredictionState?> GetPredictionStateAsync(Guid hashId,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var state = await connection
            .GetTable<Models.States.PredictionState>()
            .SingleOrDefaultAsync(state => state.HashId == hashId, token: cancellationToken);

        return state;
    }

    public async ValueTask<IReadOnlyCollection<Models.States.PredictionState>> GetPredictionStatesInProcessingAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var states = await connection
            .GetTable<Models.States.PredictionState>()
            .Where(state => state.IsInProcessing)
            .ToArrayAsync(token: cancellationToken);

        return states;
    }

    public async ValueTask<Models.Rosstat.Report> GetRosstatReportAsync(Guid hashId,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var report = await connection
            .GetTable<Models.Rosstat.Report>()
            .SingleAsync(report => report.HashId == hashId, token: cancellationToken);

        report.ValuesDictionary = JsonSerializer.Deserialize<IReadOnlyDictionary<string, long>>(report.Values)!;

        return report;
    }

    public async ValueTask InsertBankruptcyPredictionsAsync(
        IReadOnlyCollection<Models.Predictions.BankruptcyPrediction> predictions, CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        foreach (var prediction in predictions)
        {
            prediction.ChangeDate = DateTime.UtcNow;

            if (await connection
                    .GetTable<Models.Predictions.BankruptcyPrediction>()
                    .ContainsAsync(prediction, token: cancellationToken))
                await connection.UpdateAsync(prediction, token: cancellationToken);
            else
                await connection.InsertAsync(prediction, token: cancellationToken);
        }
    }

    internal class TestDataConnection : DataConnection
    {
        public TestDataConnection(string connectionString) : base(new DataOptions()
            .UseSqlServer(connectionString, SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient)
            .UseMappingSchema(new TestMappingSchema()))
        {
        }
    }

    internal class TestMappingSchema : MappingSchema
    {
        public TestMappingSchema()
        {
            var builder = new FluentMappingBuilder(this);

            builder.Entity<Models.States.DownloadingState>()
                .HasSchemaName(nameof(Models.States))
                .HasTableName(nameof(Models.States.DownloadingState)[..^5])
                .Property(state => state.RegistryId).IsPrimaryKey()
                .Property(state => state.FileName).IsPrimaryKey();
            builder.Entity<Models.States.ParsingState>()
                .HasSchemaName(nameof(Models.States))
                .HasTableName(nameof(Models.States.ParsingState)[..^5])
                .Property(state => state.RegistryId).IsPrimaryKey()
                .Property(state => state.FileName).IsPrimaryKey();
            builder.Entity<Models.States.PredictionState>()
                .HasSchemaName(nameof(Models.States))
                .HasTableName(nameof(Models.States.PredictionState)[..^5])
                .Property(state => state.HashId).IsPrimaryKey();

            builder.Entity<Models.Raw.DatasetFile>()
                .HasSchemaName(nameof(Models.Raw))
                .Property(file => file.RegistryId).IsPrimaryKey()
                .Property(file => file.FileName).IsPrimaryKey();

            builder.Entity<Models.Rosstat.Report>()
                .HasSchemaName(nameof(Models.Rosstat))
                .Property(report => report.HashId).IsPrimaryKey()
                .Property(report => report.Type).IsNullable(false)
                .Property(report => report.ValuesDictionary).IsNotColumn();

            builder.Entity<Models.Predictions.BankruptcyPrediction>()
                .HasSchemaName(nameof(Models.Predictions))
                .Property(prediction => prediction.HashId).IsPrimaryKey()
                .Property(prediction => prediction.Model).IsNullable(false)
                .Property(prediction => prediction.Probability).IsNullable(false);

            builder.Build();

            SetDefaultFromEnumType(typeof(Enum), typeof(string));
        }
    }
}

internal record TestDatabaseSettings
{
    [Required]
    public string ConnectionString { get; set; } = null!;
}
