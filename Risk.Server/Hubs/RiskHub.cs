﻿using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Risk.Game;
using Microsoft.Extensions.Configuration;
using Risk.Shared;

namespace Risk.Server.Hubs
{
    public class RiskHub : Hub<IRiskHub>
    {
        private readonly ILogger<RiskHub> logger;
        private readonly IConfiguration config;
        public const int MaxFailedTries = 5;

        private Player currentPlayer => (game.CurrentPlayer as Player);

        private Risk.Game.Game game { get; set; }
        public RiskHub(ILogger<RiskHub> logger, IConfiguration config, Game.Game game)
        {
            this.logger = logger;
            this.config = config;
            this.game = game;
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

        public async Task Signup(string user)
        {
            var duplicatePlayer = game.Players.ToList().FirstOrDefault(player => player.Token == Context.ConnectionId);
            if(duplicatePlayer != null)
            {
                await Clients.Client(duplicatePlayer.Token).SendMessage("Server", $"There is already a player registered on your client named {duplicatePlayer.Name}");
                (duplicatePlayer as Player).InvalidRequests++;
            }
            else
            {
                var sameNames = game.Players.Where(p => p.Name == user);
                if(sameNames.Count() != 0)
                {
                    user = string.Concat(user, (sameNames.Count()+1).ToString());
                }
                logger.LogInformation(Context.ConnectionId.ToString() + ": " + user);
                var newPlayer = new Player(Context.ConnectionId, user);
                game.AddPlayer(newPlayer);
                await BroadCastMessage(newPlayer.Name + " has joined the game");
                await Clients.Client(newPlayer.Token).SendMessage("Server", "Welcome to the game " + newPlayer.Name);
            }
        }

        private async Task BroadCastMessage(string message)
        {
            await Clients.All.SendMessage("Server", message);
        }

        public async Task GetStatus()
        {
            await Clients.Client(Context.ConnectionId).SendMessage("Server", game.GameState.ToString());
            await Clients.Client(Context.ConnectionId).SendStatus(game.GetGameStatus());
        }

        public async Task StartGame(string Password)
        {
            if (Password == config["StartGameCode"])
            {
                await BroadCastMessage("The Game has started");
                game.StartGame();
                await StartDeployPhase();
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendMessage("Server", "Incorrect password");
            }
        }
        private async Task StartDeployPhase()
        {
            game.CurrentPlayer = game.Players.First();

            await Clients.Client(currentPlayer.Token).YourTurnToDeploy(game.Board.SerializableTerritories);
        }


        public async Task DeployRequest(Location l)
        {
            logger.LogInformation("Received DeployRequest from {connectionId}", Context.ConnectionId);

            if(Context.ConnectionId == currentPlayer.Token)
            {
                if(currentPlayer.InvalidRequests >= MaxFailedTries)
                {
                    logger.LogInformation("{currentPlayer} has too many invalid requests.  Booting from game.", currentPlayer);
                    await Clients.Client(Context.ConnectionId).SendMessage("Server", $"Too many bad requests. No risk for you");
                    game.RemovePlayerByToken(currentPlayer.Token);
                    game.RemovePlayerFromBoard(currentPlayer.Token);
                    await tellNextPlayerToDeploy();
                    return;
                }

                if(game.TryPlaceArmy(Context.ConnectionId, l))
                {
                    await Clients.Client(Context.ConnectionId).SendMessage("Server", $"Successfully Deployed At {l.Row}, {l.Column}");
                    logger.LogInformation("{currentPlayer} deployed at {l}", currentPlayer, l);

                    if(game.GameState == GameState.Deploying)
                    {
                        logger.LogInformation("Telling next player to deploy.");
                        await tellNextPlayerToDeploy();
                    }
                    else
                    {
                        logger.LogInformation("All armies that can be deployed have been deployed.  Beginning attack state.");
                        await StartAttackPhase();
                    }
                }
                else
                {
                    logger.LogInformation("{currentPlayer} tried to deploy at {l} but deploy failed.  Increasing invalid requests.", currentPlayer, l);
                    await Clients.Client(Context.ConnectionId).SendMessage("Server", "Did not deploy successfully");
                    currentPlayer.InvalidRequests++;
                    await Clients.Client(currentPlayer.Token).YourTurnToDeploy(game.Board.SerializableTerritories);
                }
            }
            else
            {
                var badPlayer = game.Players.Single(p => p.Token == Context.ConnectionId) as Player;
                badPlayer.InvalidRequests++;
                await Clients.Client(badPlayer.Token).SendMessage("Server", "It's not your turn");
                logger.LogInformation("{currentPlayer} tried to deploy when it wasn't their turn.  Increading invalid request count.", currentPlayer);
            }
        }

        private async Task tellNextPlayerToDeploy()
        {
            var players = game.Players.ToList();
            var currentPlayerIndex = players.IndexOf(game.CurrentPlayer);
            var nextPlayerIndex = currentPlayerIndex + 1;
            if (nextPlayerIndex >= players.Count)
            {
                nextPlayerIndex = 0;
            }
            game.CurrentPlayer = players[nextPlayerIndex];
            await Clients.Client(currentPlayer.Token).YourTurnToDeploy(game.Board.SerializableTerritories);
        }

        private async Task StartAttackPhase()
        {
            game.CurrentPlayer = game.Players.First();

            await Clients.Client(currentPlayer.Token).YourTurnToAttack(game.Board.SerializableTerritories);
        }

        public async Task AttackRequest(Location from, Location to)
        {
            if (Context.ConnectionId == currentPlayer.Token)
            {
                game.OutstandingAttackRequestCount--;

                if (currentPlayer.InvalidRequests >= MaxFailedTries)
                {
                    await Clients.Client(Context.ConnectionId).SendMessage("Server", $"Too many bad requests. No risk for you");
                    game.RemovePlayerByToken(currentPlayer.Token);
                    game.RemovePlayerFromBoard(currentPlayer.Token);
                    await tellNextPlayerToAttack();
                    return;
                }

                if (game.Players.Count() > 1 && game.GameState == GameState.Attacking && game.Players.Any(p => game.PlayerCanAttack(p)))
                {
                    if (game.PlayerCanAttack(currentPlayer))
                    {
                        TryAttackResult attackResult = new TryAttackResult { AttackInvalid = false };
                        Territory attackingTerritory = null;
                        Territory defendingTerritory = null;
                        try
                        {
                            attackingTerritory = game.Board.GetTerritory(from);
                            defendingTerritory = game.Board.GetTerritory(to);

                            logger.LogInformation($"{currentPlayer.Name} wants to attack from {attackingTerritory} to {defendingTerritory}");

                            attackResult = game.TryAttack(currentPlayer.Token, attackingTerritory, defendingTerritory);
                        }
                        catch (Exception ex)
                        {
                            attackResult = new TryAttackResult { AttackInvalid = true, Message = ex.Message };
                        }
                        if (attackResult.AttackInvalid)
                        {
                            logger.LogError($"Invalid attack request! {currentPlayer.Name} from {attackingTerritory} to {defendingTerritory} ");
                            currentPlayer.InvalidRequests++;
                            await Clients.Client(currentPlayer.Token).YourTurnToAttack(game.Board.SerializableTerritories);
                        }
                        else
                        {
                            await Clients.Client(Context.ConnectionId).SendMessage("Server", $"Successfully Attacked From ({from.Row}, {from.Column}) To ({to.Row}, {to.Column})");

                            if (game.GameState == GameState.Attacking)
                            {
                                if (game.PlayerCanAttack(currentPlayer))
                                    await Clients.Client(currentPlayer.Token).YourTurnToAttack(game.Board.SerializableTerritories);
                                else
                                    await tellNextPlayerToAttack();
                            }
                            else
                            {
                                await sendGameOverAsync();
                            }
                        }
                    }
                    else
                    {
                        await Clients.Client(currentPlayer.Token).SendMessage("Server", "You are unable to attack.  Moving to next player.");
                        logger.LogInformation("Player {currentPlayer} cannot attack.", currentPlayer);
                        await tellNextPlayerToAttack();
                    }
                }
                else
                {
                    await sendGameOverAsync();
                }
            }
            else
            {
                var badPlayer = game.Players.Single(p => p.Token == Context.ConnectionId) as Player;
                badPlayer.InvalidRequests++;
                await Clients.Client(badPlayer.Token).SendMessage("Server", "It's not your turn");
            }
        }       

        public async Task AttackComplete()
        {
            await tellNextPlayerToAttack();
        }

        private async Task tellNextPlayerToAttack()
        {
            var players = game.Players.ToList();
            if (game.OutstandingAttackRequestCount >= players.Count * Game.Game.MaxTimesAPlayerCanNotAttack)
            {
                logger.LogInformation("Too many plays skipped attacking, ending game");
                await sendGameOverAsync();
                return;
            }
            game.OutstandingAttackRequestCount++;
            var currentPlayerIndex = players.IndexOf(game.CurrentPlayer);
            var nextPlayerIndex = currentPlayerIndex + 1;
            if (nextPlayerIndex >= players.Count)
            {
                nextPlayerIndex = 0;
            }
            game.CurrentPlayer = players[nextPlayerIndex];
            await Clients.Client(currentPlayer.Token).YourTurnToAttack(game.Board.SerializableTerritories);
        }

        private async Task sendGameOverAsync()
        {
            logger.LogInformation("Game Over. {gameStatus}", game.GetGameStatus());
            await BroadCastMessage($"Game Over - {game.GetGameStatus().PlayerStats.OrderByDescending(s => s.Score).First().Name} wins!");
        }



        //public async void AttackRequest(Location from, Location to)
        //{
        //    //verify they can attack, if so roll for attack, if not ask user again or skip
        //    if(game.TryAttack(players.First(p => p.ConnectionId == Context.ConnectionId).Token, ))
        //}

        public async void ContinueAttackRequest(Location from, Location to)
        {
            //verify they are attacking where they say they are, if so, continue attacking, if not ask again or skip
        }

        public async void CeaseAttackingRequest(Location from, Location to)
        {

        }

        

    }
}