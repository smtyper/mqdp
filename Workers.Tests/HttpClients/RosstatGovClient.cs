using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;

namespace Workers.Tests.HttpClients;

internal partial class RosstatGovClient
{
    private readonly HttpClient _httpClient;

    public RosstatGovClient(HttpClient httpClient) => _httpClient = httpClient;

    public async IAsyncEnumerable<(string RegistryId, string Url)> GetFinancialRegistriesAsync()
    {
        await using var registriesStream = await _httpClient.GetStreamAsync("opendata/list.csv");
        using var streamReader = new StreamReader(registriesStream);

        using var csvReader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null
        });

        var registryIdRegex = RegistryIdRegex();
        var registries = csvReader.GetRecordsAsync(new
            {
                property = (string?)null,
                title = (string?)null,
                value = (string?)null,
                format = (string?)null
            })
            .Where(record => registryIdRegex.IsMatch(record.property!) && record.format is "csv");

        await foreach (var registry in registries)
            yield return (registry.property!, registry.value!);
    }

    public async IAsyncEnumerable<(string RegistryId, string FileName)> GetFilesFromRegistryAsync(string registryId,
        string registryUrl)
    {
        await using var registryStream = await _httpClient.GetStreamAsync(registryUrl);
        using var streamReader = new StreamReader(registryStream);

        using var csvReader = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture));
        var records = csvReader
            .GetRecordsAsync(new { property = (string?)null, value = (string?)null })
            .Where(record => record.property!.StartsWith("data"));

        await foreach (var record in records)
            yield return (registryId, record.property!);
    }

    public async Task<Stream> GetFileStreamAsync(string fileUrl, CancellationToken cancellationToken) =>
        await _httpClient.GetStreamAsync(fileUrl, cancellationToken);

    [GeneratedRegex("(7708234640-?bdboo\\d{4})")]
    private static partial Regex RegistryIdRegex();
}
