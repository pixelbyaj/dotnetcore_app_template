using Microsoft.Extensions.Hosting.WindowsServices;
using NLog.Extensions.Logging;
using StartupServices.Helper;
using StartupServices;
using StartupServices.Processors;
using StartupServices.Interface;

var cmdArguments = ConfigHelper.ParseCommandline(args);

var config = ConfigHelper.SetConfiguration(cmdArguments);
ConfigHelper.SetNLogConfiguration(cmdArguments["configPath"]);

IHost host =
    Host.CreateDefaultBuilder(args)
    .UseWindowsService()
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
        if (!cmdArguments.ContainsKey("console"))
        {
            #pragma warning disable CA1416 // Validate platform compatibility
            services.AddSingleton<IHostLifetime, WindowsServiceLifetime>();
            #pragma warning restore CA1416 // Validate platform compatibility
        }
    })
    .Build();
await host.RunAsync();