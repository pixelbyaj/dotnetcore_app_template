using Microsoft.Extensions.Hosting.WindowsServices;
using NLog.Extensions.Logging;
using StartupServices.Helper;
using StartupServices;
using StartupServices.Processors;
using StartupServices.Interface;

var cmdArguments = ConfigHelper.ParseCommandline(args);

var config = ConfigHelper.SetConfiguration(cmdArguments);
ConfigHelper.SetNLogConfiguration(cmdArguments["config-path"]);

IHostBuilder hostBuilder =
    Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(a => a.AddConfiguration(config))
    .UseContentRoot(AppContext.BaseDirectory)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddLogging(loggingBuilder =>
        {
            // configure Logging with NLog
            loggingBuilder.ClearProviders();
            loggingBuilder.AddNLog(config);
        });

        services.AddRabbitMQ(config);
        services.AddKafka(config);
    });

    if (!cmdArguments.ContainsKey("console"))
    {
        hostBuilder.UseWindowsService()
        .ConfigureServices(services =>
        {
            #pragma warning disable CA1416 // Validate platform compatibility
            services.AddSingleton<IHostLifetime, WindowsServiceLifetime>();
            #pragma warning restore CA1416 // Validate platform compatibility
        });

    }
IHost host = hostBuilder.Build();
await host.RunAsync();