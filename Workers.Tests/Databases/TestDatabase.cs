using System.ComponentModel.DataAnnotations;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using Microsoft.Extensions.Options;

namespace Workers.Tests.Databases;

public class TestDatabase
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

    public async ValueTask SetDownloadingStateAsync(string fileName, DateTime? dataDate,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var state = new Models.States.DownloadingState { FileName = fileName };

        if (dataDate is null)
            await connection.DeleteAsync(state, token: cancellationToken);
        else if (await connection
                     .GetTable<Models.States.DownloadingState>()
                     .ContainsAsync(state, token: cancellationToken))
            await connection.UpdateAsync(state with { DataDate = dataDate.Value }, token: cancellationToken);
        else
            await connection.InsertAsync(state with { DataDate = dataDate.Value }, token: cancellationToken);
    }

    public async ValueTask InsertRawFilesFileAsync(Models.RawFiles.File file, CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        if (await connection.GetTable<Models.RawFiles.File>().ContainsAsync(file, token: cancellationToken))
            await connection.UpdateAsync(file, token: cancellationToken);
        else
            await connection.InsertAsync(file, token: cancellationToken);
    }

    public async ValueTask SetParsingStateAsync(string fileName, DateTime dataDate, bool isInProcessing,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var state = new Models.States.ParsingState
        {
            FileName = fileName,
            DataDate = dataDate,
            IsInProcessing = isInProcessing
        };

        if (await connection.GetTable<Models.States.ParsingState>().ContainsAsync(state, token: cancellationToken))
            await connection.UpdateAsync(state, token: cancellationToken);
        else
            await connection.InsertAsync(state, token: cancellationToken);
    }

    public async ValueTask<Models.States.ParsingState?> GetParsingStateAsync(string fileName,
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var state = await connection
            .GetTable<Models.States.ParsingState>()
            .SingleOrDefaultAsync(state => state.FileName == fileName, token: cancellationToken);

        return state;
    }

    public async ValueTask<IReadOnlyCollection<Models.States.ParsingState>> GetParsingStateInProcessingAsync(
        CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        var states = await connection
            .GetTable<Models.States.ParsingState>()
            .Where(state => state.IsInProcessing)
            .ToArrayAsync(token: cancellationToken);

        return states;
    }

    public async ValueTask InsertFileSumAsync(Models.Calculations.FileSum fileSum, CancellationToken cancellationToken)
    {
        await using var connection = new TestDataConnection(_settings.ConnectionString);

        if (await connection.GetTable<Models.Calculations.FileSum>().ContainsAsync(fileSum, token: cancellationToken))
            await connection.UpdateAsync(fileSum with { ChangeDate = DateTime.UtcNow }, token: cancellationToken);
        else
            await connection.InsertAsync(fileSum with { ChangeDate = DateTime.UtcNow }, token: cancellationToken);
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

            builder.Entity<Models.RawFiles.File>()
                .HasSchemaName(nameof(Models.RawFiles))
                .Property(file => file.FileName).IsPrimaryKey();

            builder.Entity<Models.Calculations.FileSum>()
                .HasSchemaName(nameof(Models.Calculations))
                .Property(file => file.FileName).IsPrimaryKey();

            builder.Entity<Models.States.DownloadingState>()
                .HasSchemaName(nameof(Models.States))
                .HasTableName(nameof(Models.States.DownloadingState)[..^5])
                .Property(state => state.FileName).IsPrimaryKey();
            builder.Entity<Models.States.ParsingState>()
                .HasSchemaName(nameof(Models.States))
                .HasTableName(nameof(Models.States.ParsingState)[..^5])
                .Property(state => state.FileName).IsPrimaryKey();

            builder.Build();

            SetDefaultFromEnumType(typeof(Enum), typeof(string));
        }
    }
}

public record TestDatabaseSettings
{
    [Required]
    public string ConnectionString { get; set; } = null!;
}
