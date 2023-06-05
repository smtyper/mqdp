using System.Diagnostics;
using LinqToDB.Data;
using Workers;
using Workers.Tests.Databases;
using Workers.Tests.HttpClients;
using Workers.Tests.Workers;
using Workers.Tests.Workers.Channels;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        if (hostContext.HostingEnvironment.IsDevelopment())
        {
            DataConnection.TurnTraceSwitchOn();
            DataConnection.WriteTraceLine = (message, category, _) => Debug.WriteLine(message, category);
        }

        services.AddHttpClient<RosstatGovClient>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri("https://rosstat.gov.ru/"));

        services.AddSingleton<TestDatabase>().AddOptions<TestDatabaseSettings>()
            .Configure(settings => settings.ConnectionString = hostContext.Configuration.GetConnectionString("Test")!)
            .ValidateDataAnnotations();

        services.AddSingleton<ParsingChannel>();
        services.AddSingleton<PredictionChannel>();

        services.AddHostedService<DownloadingWorker, DownloadingSettings>()
            .ConfigureAsWorker(hostContext.Configuration);
        services.AddHostedService<ParsingWorker, ParsingSettings>()
            .ConfigureAsWorker(hostContext.Configuration);
        services.AddHostedService<PredictionWorker, PredictionSettings>()
            .ConfigureAsWorker(hostContext.Configuration);
    })
    .ConfigureWorkers()
    .Build();

await host.RunInWrapperAsync();
