using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Risk.Signalr.ConsoleClient
{
    public abstract class HostedPlayerLogic : IHostedService, IPlayerLogic
    {
        private const string DefaultServerAddress = "http://localhost:5000";
        static HubConnection hubConnection;
        private readonly IConfiguration config;
        private readonly IHostApplicationLifetime applicationLifetime;

        public HostedPlayerLogic(IConfiguration configuration, IHostApplicationLifetime applicationLifetime)
        {
            this.config = configuration;
            this.applicationLifetime = applicationLifetime;
        }

        public abstract string MyPlayerName { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            //applicationLifetime.ApplicationStarted.Register(OnStarted);
            //applicationLifetime.ApplicationStopping.Register(OnStopping);
            //applicationLifetime.ApplicationStopped.Register(OnStopped);

            var serverAddress = config["serverAddress"] ?? DefaultServerAddress;
            Console.WriteLine($"Talking to the server at {serverAddress}");

            hubConnection = new HubConnectionBuilder()
                .WithUrl($"{serverAddress}/riskhub")
                .Build();

            hubConnection.On<string, string>(MessageTypes.SendMessage, (from, message) => Console.WriteLine("From {0}: {1}", from, message));

            hubConnection.On<string>(MessageTypes.JoinConfirmation, validatedName =>
            {
                Console.Title = validatedName;
                MyPlayerName = validatedName;
                Console.WriteLine($"Successfully joined server. Assigned Name is {validatedName}");
            });

            hubConnection.On<IEnumerable<BoardTerritory>>(MessageTypes.YourTurnToDeploy, async (board) =>
            {
                var deployLocation = WhereDoYouWantToDeploy(board);
                Console.WriteLine("Deploying to {0}", deployLocation);
                await DeployAsync(deployLocation);
            });

            hubConnection.On<IEnumerable<BoardTerritory>>(MessageTypes.YourTurnToAttack, async (board) =>
            {
                try
                {
                    (var from, var to) = WhereDoYouWantToAttack(board);
                    Console.WriteLine("Attacking from {0} ({1}) to {2} ({3})", from, board.First(c => c.Location == from).OwnerName, to, board.First(c => c.Location == to).OwnerName);
                    await AttackAsync(from, to);
                }
                catch
                {
                    Console.WriteLine("Yielding turn (nowhere left to attack)");
                    await AttackCompleteAsync();
                }
            });

            hubConnection.Closed += (ex) =>
            {
                Console.WriteLine("\nConnection terminated.  Closing program.");
                applicationLifetime.StopApplication();
                return Task.CompletedTask;
            };

            Console.ReadLine();
            await hubConnection.StartAsync();

            Console.WriteLine("My connection id is {0}.  Waiting for game to start...", hubConnection.ConnectionId);
            await SignupAsync(MyPlayerName);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        static async Task SignupAsync(string playerName) => await hubConnection.SendAsync(MessageTypes.Signup, playerName);

        static async Task DeployAsync(Location desiredLocation) => await hubConnection.SendAsync(MessageTypes.DeployRequest, desiredLocation);

        static async Task AttackAsync(Location from, Location to) => await hubConnection.SendAsync(MessageTypes.AttackRequest, from, to);

        static async Task AttackCompleteAsync() => await hubConnection.SendAsync(MessageTypes.AttackComplete);

        public abstract (Location from, Location to) WhereDoYouWantToAttack(IEnumerable<BoardTerritory> board);

        public abstract Location WhereDoYouWantToDeploy(IEnumerable<BoardTerritory> board);

        protected IEnumerable<BoardTerritory> GetNeighbors(BoardTerritory territory, IEnumerable<BoardTerritory> board)
        {
            var l = territory.Location;
            var neighborLocations = new[] {
                new Location(l.Row+1, l.Column-1),
                new Location(l.Row+1, l.Column),
                new Location(l.Row+1, l.Column+1),
                new Location(l.Row, l.Column-1),
                new Location(l.Row, l.Column+1),
                new Location(l.Row-1, l.Column-1),
                new Location(l.Row-1, l.Column),
                new Location(l.Row-1, l.Column+1),
            };
            return board.Where(t => neighborLocations.Contains(t.Location));
        }
    }
}
