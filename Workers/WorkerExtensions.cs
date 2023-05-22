using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MoreLinq;
using Serilog;
using Serilog.Debugging;

namespace Workers;

public static class WorkerExtensions
{
    public static IHostBuilder ConfigureWorkers(this IHostBuilder builder) =>
        (builder ?? throw new ArgumentNullException(nameof(builder)))
        .ConfigureAppConfiguration((hostContext, configurationBuilder) =>
        {
            const string defaultApplicationGroup = "workers";
            var defaultApplicationName = hostContext.HostingEnvironment.ApplicationName
                .Replace('.', '-')
                .ToLowerInvariant();

            var appsettings = configurationBuilder.Sources
                .OfType<FileConfigurationSource>()
                .FirstOrDefault(s => Path.GetFileNameWithoutExtension(s.Path) == "appsettings");

            configurationBuilder.Sources.Insert(
                appsettings is null ? 0 : configurationBuilder.Sources.IndexOf(appsettings),
                new MemoryConfigurationSource
                {
                    InitialData = new Dictionary<string, string?>
                    {
                        ["Serilog:MinimumLevel:Default"] = "Information",
                        ["Serilog:MinimumLevel:Override:System"] = "Warning",
                        ["Serilog:MinimumLevel:Override:Microsoft"] = "Warning",
                        ["Serilog:MinimumLevel:Override:Microsoft.Hosting.Lifetime"] = "Information",
                        ["Serilog:Enrich:0"] = "FromLogContext",
                        ["Serilog:Enrich:1"] = "WithAssemblyName",
                        ["Serilog:Enrich:2"] = "WithAssemblyVersion",
                        ["Serilog:Enrich:3"] = "WithMachineName",
                        ["Serilog:Enrich:4"] = "WithEnvironmentUserName",
                        ["Serilog:Properties:ApplicationSessionId"] = Guid.NewGuid().ToString(),
                        ["Serilog:WriteTo:DefaultHttp:Name"] = "Http",
                        ["Serilog:WriteTo:DefaultHttp:Args:requestUri"] = null,
                        ["Serilog:WriteTo:DefaultHttp:Args:queueLimitBytes"] = null,
                        ["Serilog:WriteTo:DefaultHttp:Args:httpClient"] =
                            "Reputation.Workers.LogstashHttpClient, Reputation.Workers",
                        ["Serilog:WriteTo:DefaultHttp:Headers:ApplicationGroup"] = defaultApplicationGroup,
                        ["Serilog:WriteTo:DefaultHttp:Headers:ApplicationName"] = defaultApplicationName,
                        ["Serilog:WriteTo:DefaultConsole:Name"] = "Logger",
                        ["Serilog:WriteTo:DefaultConsole:Args:configureLogger:WriteTo:DefaultSubConsole:Name"] =
                            "Console",
                        ["Serilog:WriteTo:DefaultConsole:Args:configureLogger:Filter:DefaultFilter:Name"] =
                            "ByIncludingOnly",
                        ["Serilog:WriteTo:DefaultConsole:Args:configureLogger:Filter:DefaultFilter:Args:expression"] =
                            "@l in ['Fatal'] or SourceContext = 'Microsoft.Hosting.Lifetime' or @mt = '{WorkerName} initializing'"
                    }
                });

            var appsettingsDevelopment = configurationBuilder.Sources
                .OfType<FileConfigurationSource>()
                .FirstOrDefault(s => Path.GetFileNameWithoutExtension(s.Path) == "appsettings.Development");

            if (!(appsettingsDevelopment is null))
                configurationBuilder.Sources.Insert(configurationBuilder.Sources.IndexOf(appsettingsDevelopment),
                    new MemoryConfigurationSource
                    {
                        InitialData = new Dictionary<string, string?>
                        {
                            ["Serilog:MinimumLevel:Override:System"] = "Information",
                            ["Serilog:MinimumLevel:Override:Microsoft"] = "Information",
                            ["Serilog:WriteTo:DefaultHttp"] = string.Empty,
                            ["Serilog:WriteTo:DefaultConsole:Args:configureLogger:Filter:DefaultFilter"] =
                                string.Empty
                        }
                    });
        })
        .ConfigureServices((hostContext, services) =>
            services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromMinutes(1)))
        .RemoveDisabledWorkers()
        .UseSerilog((hostContext, loggerConfiguration) =>
        {
            if (!hostContext.HostingEnvironment.IsDevelopment())
                SelfLog.Enable(Console.Error);

            loggerConfiguration.ReadFrom.Configuration(hostContext.Configuration);

            // Enableing after initialization to ignore configuration exceptions in development
            if (hostContext.HostingEnvironment.IsDevelopment())
                SelfLog.Enable(Console.Error);
        });

    private static IHostBuilder RemoveDisabledWorkers(this IHostBuilder builder) => builder
        .ConfigureServices((hostContext, services) =>
        {
            var disabledWorkers = (
                from service in services
                let type = service.ImplementationType ?? service.ImplementationFactory?.Method?.ReturnType
                where type.IsAssignableToGenericType(typeof(Worker<,>))
                let isWorkerEnabled = hostContext.Configuration.GetIsWorkerEnabledValue(type.Name)
                where isWorkerEnabled.HasValue && !isWorkerEnabled.Value
                select service
            ).ToArray();

            foreach (var worker in disabledWorkers)
                services.Remove(worker);
        });

    /// <summary>
    /// Runs an application and returns a Task that only completes when the token is
    /// triggered or shutdown is triggered. Logs unhandled exceptions.
    /// </summary>
    /// <param name="host">The <see cref="IHost" /> to run.</param>
    /// <param name="token">The token to trigger shutdown.</param>
    /// <returns>Exit code.</returns>
    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification =
        "Log critical exception and return exit code that indicates failure.")]
    public static async Task<int> RunInWrapperAsync(this IHost host, CancellationToken token = default)
    {
        var logger = host.Services.GetRequiredService<ILogger>();

        try
        {
            await host.StartAsync(token).ConfigureAwait(false);
            await host.WaitForShutdownAsync(token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Fatal(ex, "Program terminated unexpectedly");

            return 1;
        }
        finally
        {
            if (host is IAsyncDisposable asyncDisposableHost)
                await asyncDisposableHost.DisposeAsync().ConfigureAwait(false);
            else
                host.Dispose();
        }

        return 0;
    }

    public static OptionsBuilder<TOptions> AddHostedService<THostedService, TOptions>(
        this IServiceCollection services) where THostedService : class, IConfigurableHostedService
        where TOptions : class =>
        services.AddHostedService<THostedService>().AddOptions<TOptions>(typeof(THostedService).Name);

    public static OptionsBuilder<TOptions> AddHostedService<THostedService, TOptions>(
        this IServiceCollection services, Func<IServiceProvider, THostedService> implementationFactory)
        where THostedService : class, IConfigurableHostedService where TOptions : class =>
        services.AddHostedService(implementationFactory).AddOptions<TOptions>(typeof(THostedService).Name);

    public static OptionsBuilder<TOptions> ConfigureAsWorker<TOptions>(this OptionsBuilder<TOptions> optionsBuilder,
        IConfiguration configuration) where TOptions : WorkerSettings =>
        (optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder))).Services
        .AddOptions<TOptions>()
        .Bind((configuration ?? throw new ArgumentNullException(nameof(optionsBuilder)))
            .GetSection($@"Workers:{optionsBuilder.Name.TrimEnd("Worker")}"))
        .Configure(o => o.IsEnabled = configuration.GetIsWorkerEnabledValue(optionsBuilder.Name) ?? o.IsEnabled)
        .ValidateDataAnnotations();
}
