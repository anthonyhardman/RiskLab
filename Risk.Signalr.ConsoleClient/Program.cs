using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Risk.Signalr.ConsoleClient;
using System.Linq;

await Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        if (args.Any(a => a == "AnthonyBreanna"))
        {
            services.AddHostedService<MyPlayerLogic>();
        }
        else
        {
            services.AddHostedService<DefaultHostedPlayerLogic>();
        }
        
    })
    .RunConsoleAsync();
