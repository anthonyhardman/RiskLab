using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Risk.Game;
using Microsoft.Extensions.Configuration;
using Risk.Shared;
using Akka.Actor;
using Risk.Akka;

namespace Risk.Server.Hubs
{
    public class RiskHub : Hub<IRiskHub>
    {
        private readonly ILogger<RiskHub> logger;
        private readonly IConfiguration config;
        private readonly ActorSystem actorSystem;


        public RiskHub(ILogger<RiskHub> logger, IConfiguration config, ActorSystem actorSystem)
        {
            this.logger = logger;
            this.config = config;
            this.actorSystem = actorSystem;
        }
        public override async Task OnConnectedAsync()
        {
            logger.LogInformation(Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendMessage(user, message);
        }

        public async Task Signup(string requestedName)
        {
            await Task.FromResult(false);
            actorSystem.ActorSelection(ActorConstants.IOActorName).Tell(new SignupMessage(requestedName, Context.ConnectionId));
        }

        private async Task BroadCastMessage(string message)
        {
            //await Clients.All.SendMessage("Server", message);
        }

        public async Task GetStatus()
        {
            //await Clients.Client(Context.ConnectionId).SendMessage("Server", game.GameState.ToString());
            //await Clients.Client(Context.ConnectionId).SendStatus(game.GetGameStatus());
        }

        public async Task StartGame(string Password)
        {
            //if (Password == config["StartGameCode"])
            //{
            //    await BroadCastMessage("The Game has started");
            //    game.StartGame();
            //    await StartDeployPhase();
            //}
            //else
            //{
            //    await Clients.Client(Context.ConnectionId).SendMessage("Server", "Incorrect password");
            //}
        }
        private async Task StartDeployPhase()
        {
            //game.CurrentPlayer = game.Players.First();

            //await Clients.Client(currentPlayer.Token).YourTurnToDeploy(game.Board.SerializableTerritories);
        }


        public async Task DeployRequest(Location l)
        {
            //logger.LogInformation("Received DeployRequest from {connectionId}", Context.ConnectionId);

            //if (Context.ConnectionId == currentPlayer.Token)
            //{
            //    if (currentPlayer.InvalidRequests >= MaxFailedTries)
            //    {
            //        logger.LogInformation("{currentPlayer} has too many invalid requests.  Booting from game.", currentPlayer);
            //        await Clients.Client(Context.ConnectionId).SendMessage("Server", $"Too many bad requests. No risk for you");
            //        game.RemovePlayerByToken(currentPlayer.Token);
            //        game.RemovePlayerFromBoard(currentPlayer.Token);
            //        await tellNextPlayerToDeploy();
            //        return;
            //    }

            //    if (game.TryPlaceArmy(Context.ConnectionId, l))
            //    {
            //        await Clients.All.SendStatus(game.GetGameStatus());
            //        await Clients.Client(Context.ConnectionId).SendMessage("Server", $"Successfully Deployed At {l.Row}, {l.Column}");
            //        logger.LogInformation("{currentPlayer} deployed at {l}", currentPlayer, l);

            //        if (game.GameState == GameState.Deploying)
            //        {
            //            logger.LogInformation("Telling next player to deploy.");
            //            await tellNextPlayerToDeploy();
            //        }
            //        else
            //        {
            //            logger.LogInformation("All armies that can be deployed have been deployed.  Beginning attack state.");
            //            await StartAttackPhase();
            //        }
            //    }
            //    else
            //    {
            //        logger.LogInformation("{currentPlayer} tried to deploy at {l} but deploy failed.  Increasing invalid requests.", currentPlayer, l);
            //        await Clients.Client(Context.ConnectionId).SendMessage("Server", "Did not deploy successfully");
            //        currentPlayer.InvalidRequests++;
            //        await Clients.Client(currentPlayer.Token).YourTurnToDeploy(game.Board.SerializableTerritories);
            //    }
            //}
            //else
            //{
            //    var badPlayer = game.Players.Single(p => p.Token == Context.ConnectionId) as Player;
            //    badPlayer.InvalidRequests++;
            //    await Clients.Client(badPlayer.Token).SendMessage("Server", "It's not your turn");
            //    logger.LogInformation("{currentPlayer} tried to deploy when it wasn't their turn.  Increading invalid request count.", currentPlayer);
            //}
        }

        private async Task tellNextPlayerToDeploy()
        {
            //var players = game.Players.ToList();
            //var currentPlayerIndex = players.IndexOf(game.CurrentPlayer);
            //var nextPlayerIndex = currentPlayerIndex + 1;
            //if (nextPlayerIndex >= players.Count)
            //{
            //    nextPlayerIndex = 0;
            //}
            //game.CurrentPlayer = players[nextPlayerIndex];
            //await Clients.Client(currentPlayer.Token).YourTurnToDeploy(game.Board.SerializableTerritories);
        }

        private async Task StartAttackPhase()
        {
            //game.CurrentPlayer = game.Players.First();

            //await Clients.Client(currentPlayer.Token).YourTurnToAttack(game.Board.SerializableTerritories);
        }

        public async Task AttackRequest(Location from, Location to)
        {
            //if (Context.ConnectionId == currentPlayer.Token)
            //{
            //    game.OutstandingAttackRequestCount--;

            //    if (currentPlayer.InvalidRequests >= MaxFailedTries)
            //    {
            //        await Clients.Client(Context.ConnectionId).SendMessage("Server", $"Too many bad requests. No risk for you");
            //        game.RemovePlayerByToken(currentPlayer.Token);
            //        game.RemovePlayerFromBoard(currentPlayer.Token);
            //        await tellNextPlayerToAttack();
            //        return;
            //    }

            //    if (game.Players.Count() > 1 && game.GameState == GameState.Attacking && game.Players.Any(p => game.PlayerCanAttack(p)))
            //    {
            //        if (game.PlayerCanAttack(currentPlayer))
            //        {
            //            TryAttackResult attackResult = new TryAttackResult { AttackInvalid = false };
            //            Territory attackingTerritory = null;
            //            Territory defendingTerritory = null;
            //            try
            //            {
            //                attackingTerritory = game.Board.GetTerritory(from);
            //                defendingTerritory = game.Board.GetTerritory(to);

            //                logger.LogInformation($"{currentPlayer.Name} wants to attack from {attackingTerritory} to {defendingTerritory}");

            //                attackResult = game.TryAttack(currentPlayer.Token, attackingTerritory, defendingTerritory);
            //                await Clients.All.SendStatus(game.GetGameStatus());
            //            }
            //            catch (Exception ex)
            //            {
            //                attackResult = new TryAttackResult { AttackInvalid = true, Message = ex.Message };
            //            }
            //            if (attackResult.AttackInvalid)
            //            {
            //                logger.LogError($"Invalid attack request! {currentPlayer.Name} from {attackingTerritory} to {defendingTerritory} ");
            //                currentPlayer.InvalidRequests++;
            //                await Clients.Client(currentPlayer.Token).YourTurnToAttack(game.Board.SerializableTerritories);

            //            }
            //            else
            //            {
            //                await Clients.Client(Context.ConnectionId).SendMessage("Server", $"Successfully Attacked From ({from.Row}, {from.Column}) To ({to.Row}, {to.Column})");

            //                if (game.GameState == GameState.Attacking)
            //                {
            //                    if (game.PlayerCanAttack(currentPlayer))
            //                    {
            //                        await Clients.Client(currentPlayer.Token).YourTurnToAttack(game.Board.SerializableTerritories);
            //                    }
            //                    else
            //                        await tellNextPlayerToAttack();
            //                }
            //                else
            //                {
            //                    await sendGameOverAsync();
            //                }
            //            }
            //        }
            //        else
            //        {
            //            await Clients.Client(currentPlayer.Token).SendMessage("Server", "You are unable to attack.  Moving to next player.");
            //            logger.LogInformation("Player {currentPlayer} cannot attack.", currentPlayer);
            //            await tellNextPlayerToAttack();
            //        }
            //    }
            //    else
            //    {
            //        await sendGameOverAsync();
            //    }
            //}
            //else
            //{
            //    var badPlayer = game.Players.Single(p => p.Token == Context.ConnectionId) as Player;
            //    badPlayer.InvalidRequests++;
            //    await Clients.Client(badPlayer.Token).SendMessage("Server", "It's not your turn");
            //}
        }

        

        public async Task AttackComplete()
        {
            //await tellNextPlayerToAttack();
        }

        private async Task tellNextPlayerToAttack()
        {
            //var players = game.Players.ToList();
            //if (game.OutstandingAttackRequestCount >= players.Count * Game.Game.MaxTimesAPlayerCanNotAttack)
            //{
            //    logger.LogInformation("Too many plays skipped attacking, ending game");
            //    await sendGameOverAsync();
            //    return;
            //}
            //game.OutstandingAttackRequestCount++;
            //var currentPlayerIndex = players.IndexOf(game.CurrentPlayer);
            //var nextPlayerIndex = currentPlayerIndex + 1;
            //if (nextPlayerIndex >= players.Count)
            //{
            //    nextPlayerIndex = 0;
            //}
            //game.CurrentPlayer = players[nextPlayerIndex];
            //await Clients.Client(currentPlayer.Token).YourTurnToAttack(game.Board.SerializableTerritories);
        }

        private async Task sendGameOverAsync()
        {
            //game.SetGameOver();
            //var status = game.GetGameStatus();
            //logger.LogInformation("Game Over. {gameStatus}", status);
            //var winners = status.PlayerStats.Where(s => s.Score == status.PlayerStats.Max(s => s.Score)).Select(s => s.Name);
            //await BroadCastMessage($"Game Over - {string.Join(',', winners)} win{(winners.Count() > 1 ? "" : "s")}!");
            //await Clients.All.SendStatus(game.GetGameStatus());
        }

        public async Task JoinFailed(string connectionId)
        {
            await Clients.Client(connectionId).SendMessage("Server", "Unable to join game.");
        }

        public async Task JoinConfirmation(string assignedName, string connectionId)
        {
            await Clients.Client(connectionId).JoinConfirmation(assignedName);
            await BroadCastMessage(assignedName + " has joined the game");
            await Clients.Client(connectionId).SendMessage("Server", "Welcome to the game " + assignedName);
        }
    }
}
