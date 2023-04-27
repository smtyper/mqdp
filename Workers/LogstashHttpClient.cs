using System;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Serilog.Sinks.Http.HttpClients;

namespace Workers;

public class LogstashHttpClient : JsonGzipHttpClient
{
    private readonly HttpClient _httpClient;

    public LogstashHttpClient() : this(new HttpClient())
    {
    }

    public LogstashHttpClient(HttpClient httpClient) : base(httpClient, CompressionLevel.Fastest) =>
        _httpClient = httpClient;

    public override void Configure(IConfiguration configuration)
    {
        const string applicationGroupKey = "Serilog:WriteTo:DefaultHttp:Headers:ApplicationGroup";
        const string applicationNameKey = "Serilog:WriteTo:DefaultHttp:Headers:ApplicationName";

        var applicationGroup = configuration[applicationGroupKey];
        var applicationName = configuration[applicationNameKey];

        if (string.IsNullOrEmpty(applicationGroup))
            throw new Exception($"{applicationGroupKey} is not defined.");

        if (applicationGroup.Any(char.IsUpper))
            throw new Exception($"{applicationGroupKey} should be lower case.");

        if (string.IsNullOrEmpty(applicationName))
            throw new Exception($"{applicationNameKey} is not defined.");

        if (applicationName.Any(char.IsUpper))
            throw new Exception($"{applicationNameKey} should be lower case.");

        _httpClient.DefaultRequestHeaders.Add("Application-Group", applicationGroup);
        _httpClient.DefaultRequestHeaders.Add("Application-Name", applicationName);
    }
}
