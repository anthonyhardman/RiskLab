using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Risk.Signalr.ConsoleClient;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<DefaultHostedPlayerLogic>();
    })
    .RunConsoleAsync();
