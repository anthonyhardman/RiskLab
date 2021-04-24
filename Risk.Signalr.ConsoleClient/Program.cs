﻿using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Risk.Shared;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risk.Signalr.ConsoleClient
{
    static class Program
    {
        private const string DefaultServerAddress = "http://localhost:5000";
        static HubConnection hubConnection;
        private static IPlayerLogic playerLogic;
        private static IConfiguration config;

        static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args);

        static async Task Main(string[] args)
        {
            if (args.Any(s => s == "?" || s == "help"))
            {
                Console.WriteLine("Valid arguments:");
                Console.WriteLine("\t--playerName joe");
                Console.WriteLine($"\t--serverAddress {DefaultServerAddress}");
                Console.WriteLine("\t--useAlternate true");
                return;
            }

            using IHost host = CreateHostBuilder(args).Build();
            config = host.Services.GetService(typeof(IConfiguration)) as IConfiguration ?? throw new Exception("Unable to load configuration");

            Console.WriteLine("What is your player name?");
            var playerName = config["playerName"] switch
            {
                null => Console.ReadLine(),
                string name => name
            };
            Console.WriteLine("Hello {0}!", playerName);

            var serverAddress = config["serverAddress"] ?? DefaultServerAddress;
            Console.WriteLine($"Talking to the server at {serverAddress}");

            hubConnection = new HubConnectionBuilder()
                .WithUrl($"{serverAddress}/riskhub")
                .Build();

            hubConnection.On<IEnumerable<BoardTerritory>>(MessageTypes.YourTurnToDeploy, async (board) =>
            {
                var deployLocation = playerLogic.WhereDoYouWantToDeploy(board);
                Console.WriteLine("Deploying to {0}", deployLocation);
                await DeployAsync(deployLocation);
            });

            hubConnection.On<IEnumerable<BoardTerritory>>(MessageTypes.YourTurnToAttack, async (board) =>
            {
                try
                {
                    (var from, var to) = playerLogic.WhereDoYouWantToAttack(board);
                    Console.WriteLine("Attacking from {0} ({1}) to {2} ({3})", from, board.First(c => c.Location == from).OwnerName, to, board.First(c => c.Location == to).OwnerName);
                    await AttackAsync(from, to);
                }
                catch
                {
                    Console.WriteLine("Yielding turn (nowhere left to attack)");
                    await AttackCompleteAsync();
                }
            });

            hubConnection.On<string, string>(MessageTypes.SendMessage, (from, message) => Console.WriteLine("From {0}: {1}", from, message));

            hubConnection.On<string>(MessageTypes.JoinConfirmation, validatedName =>
            {
                if (config["useAlternate"] == "true")
                {
                    playerLogic = new AlternateSampleLogic(validatedName);
                }
                else
                {
                    playerLogic = new PlayerLogic(validatedName);
                }
                Console.Title = validatedName;
                Console.WriteLine($"Successfully joined server. Assigned Name is {validatedName}");
            });

            hubConnection.On<GameStatus>(MessageTypes.SendStatus, (status) =>
            {
                Console.WriteLine($"Status: {status}");
            });

            hubConnection.Closed += HubConnection_Closed;

            await hubConnection.StartAsync();

            Console.WriteLine("My connection id is {0}.  Waiting for game to start...", hubConnection.ConnectionId);
            await SignupAsync(playerName);

            Console.ReadLine();

            Console.WriteLine("Disconnecting from server.  Game over.");
        }

        private static Task HubConnection_Closed(Exception arg)
        {
            Console.WriteLine("\nConnection terminated.  Closing program.");
            Environment.Exit(0);
            return Task.CompletedTask;
        }

        static async Task SignupAsync(string playerName) => await hubConnection.SendAsync(MessageTypes.Signup, playerName);

        static async Task DeployAsync(Location desiredLocation) => await hubConnection.SendAsync(MessageTypes.DeployRequest, desiredLocation);

        static async Task AttackAsync(Location from, Location to) => await hubConnection.SendAsync(MessageTypes.AttackRequest, from, to);

        static async Task AttackCompleteAsync() => await hubConnection.SendAsync(MessageTypes.AttackComplete);
    }
}