using Microsoft.AspNetCore.SignalR.Client;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Risk.Signalr.ConsoleClient
{
    class Program
    {
        static HubConnection hubConnection;
        private static PlayerLogic playerLogic;

        static async Task Main(string[] args)
        {
            Console.WriteLine("What is your player name?");
            var playerName = Console.ReadLine();

            var serverAddress = "http://localhost:5000";
            
            if(args.Length == 1)
            {
                serverAddress = args[0];
            }
            Console.WriteLine($"Talking to the server at {serverAddress}. Press enter to connect.");
            Console.ReadLine();

            hubConnection = new HubConnectionBuilder()
                .WithUrl($"{serverAddress}/riskhub")
                .Build();
            hubConnection.On<IEnumerable<BoardTerritory>>(MessageTypes.YourTurnToDeploy, async (board) =>
            {
                var deployLocation = playerLogic.WhereDoYouWantToDeploy(board);
                await DeployAsync(deployLocation);
            });
            hubConnection.On<IEnumerable<BoardTerritory>>(MessageTypes.YourTurnToAttack, async (board) =>
            {
                try
                {
                    (var from, var to) = playerLogic.WhereDoYouWantToAttack(board);
                    await AttackAsync(from, to);
                }
                catch
                {
                    await AttackCompleteAsync();
                }

            });
            hubConnection.On<string>(MessageTypes.JoinConfirmation, validatedName => {

                playerLogic = new PlayerLogic(validatedName);
                Console.Title = validatedName;
                Console.WriteLine($"Successfully joined server. Assigned Name is {validatedName}");
            });

            hubConnection.On<string, string>(MessageTypes.SendMessage, (name, message) =>
            {
                Console.WriteLine(name + ": " + message);
            });

            await hubConnection.StartAsync();
            Console.WriteLine("My connection id is " + hubConnection.ConnectionId);
            try
            {
                await SignupAsync(playerName);
            } catch(Exception e)
            {

            }
            Console.ReadLine();
        }

        static async Task SignupAsync(string playerName)
        {
            await hubConnection.SendAsync(MessageTypes.Signup, playerName);
        }
        static async Task DeployAsync(Location desiredLocation) => await hubConnection.SendAsync(MessageTypes.DeployRequest, desiredLocation);
        static async Task AttackAsync(Location from, Location to) => await hubConnection.SendAsync(MessageTypes.AttackRequest, from, to);
        static async Task AttackCompleteAsync() => await hubConnection.SendAsync(MessageTypes.AttackComplete);
    }
}
