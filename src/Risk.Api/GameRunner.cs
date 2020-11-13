﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Risk.Game;
using Risk.Shared;

namespace Risk.Api
{
    public class GameRunner
    {
        private readonly Game.Game game;
        private readonly IList<ApiPlayer> players;
        private readonly IList<ApiPlayer> removedPlayers;
        public const int MaxFailedTries = 5;

        public GameRunner(Game.Game game, IList<ApiPlayer> players, IList<ApiPlayer> removedPlayers)
        {
            this.game = game;
            this.players = players;
            this.removedPlayers = removedPlayers;
        }

        public async Task StartGameAsync()
        {
            await deployArmiesAsync();
            await doBattle();
            await reportWinner();
        }

        private async Task deployArmiesAsync()
        {
            while (game.Board.Territories.Sum(t => t.Armies) < game.StartingArmies * players.Count())
            {
                foreach (var currentPlayer in players)
                {
                    var deployArmyResponse = await askForDeployLocationAsync(currentPlayer, DeploymentStatus.YourTurn);

                    var failedTries = 0;
                    //check that this location exists and is available to be used (e.g. not occupied by another army)
                    while (game.TryPlaceArmy(currentPlayer.Token, deployArmyResponse.DesiredLocation) is false)
                    {
                        failedTries++;
                        if (failedTries == MaxFailedTries)
                        {
                            BootPlayerFromGame(currentPlayer);
                        }
                        deployArmyResponse = await askForDeployLocationAsync(currentPlayer, DeploymentStatus.PreviousAttemptFailed);
                    }
                }
            }
        }

        private async Task<DeployArmyResponse> askForDeployLocationAsync(ApiPlayer currentPlayer, DeploymentStatus deploymentStatus)
        {
            var deployArmyRequest = new DeployArmyRequest {
                Board = game.Board.SerializableTerritories,
                Status = deploymentStatus,
                ArmiesRemaining = game.GetPlayerRemainingArmies(currentPlayer.Token)
            };
            var json = System.Text.Json.JsonSerializer.Serialize(deployArmyRequest);
            var deployArmyResponse = (await currentPlayer.HttpClient.PostAsJsonAsync("/deployArmy", deployArmyRequest));
            deployArmyResponse.EnsureSuccessStatusCode();
            var r = await deployArmyResponse.Content.ReadFromJsonAsync<DeployArmyResponse>();
            return r;
        }

        private async Task doBattle()
        {
            game.StartTime = DateTime.Now;
            while (players.Count > 1 && game.GameState == GameState.Attacking)
            {
                bool someonePlayedThisRound = false;

                for(int i = 0; i < players.Count; i++)
                {
                    if (game.PlayerCanAttack(players[i]))
                    {
                        someonePlayedThisRound = true;
                        var failedTries = 0;

                        TryAttackResult attackResult;
                        Territory attackingTerritory;
                        Territory defendingTerritory;
                        do
                        {
                            var beginAttackResponse = await askForAttackLocationAsync(players[i], BeginAttackStatus.PreviousAttackRequestFailed);
                            attackingTerritory = new Territory(beginAttackResponse.From);
                            defendingTerritory = new Territory(beginAttackResponse.To);
                            attackResult = game.TryAttack(players[i].Token, attackingTerritory, defendingTerritory);

                            if (attackResult.AttackInvalid)
                            {
                                failedTries++;
                                if (failedTries == MaxFailedTries)
                                {
                                    RemovePlayerFromBoard(players[i].Token);
                                    RemovePlayerFromGame(players[i].Token);
                                    i--;
                                }
                            }
                        } while (attackResult.AttackInvalid);

                        while(attackResult.CanContinue)
                        {
                            var continueResponse = await askContinueAttackingAsync(players[i], attackingTerritory, defendingTerritory);
                            if (continueResponse.ContinueAttacking)
                            {
                                attackResult = game.TryAttack(players[i].Token, attackingTerritory, defendingTerritory);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }

                if(someonePlayedThisRound is false)
                {
                    game.SetGameOver();
                    return;
                }
            }
        }

        private void RemovePlayerFromGame(string token)
        {
            for (int i = 0; i < players.Count(); i++)
            {
                var player = players.ElementAt(i);
                if (player.Token == token)
                {
                    players.Remove(player);
                    removedPlayers.Add(player);
                }
            }
        }

        private async Task<BeginAttackResponse> askForAttackLocationAsync(ApiPlayer player, BeginAttackStatus beginAttackStatus)
        {
            var beginAttackRequest = new BeginAttackRequest {
                Board = game.Board.SerializableTerritories,
                Status = beginAttackStatus
            };
            return await (await player.HttpClient.PostAsJsonAsync("/beginAttack", beginAttackRequest))
                .EnsureSuccessStatusCode()
                .Content.ReadFromJsonAsync<BeginAttackResponse>();
        }

        private async Task reportWinner()
        {
            game.EndTime = DateTime.Now;
            TimeSpan gameDuration = game.EndTime - game.StartTime;

            var scores = new List<(int, ApiPlayer)>();

            foreach (var currentPlayer in players)
            {
                var playerScore = 2 * game.GetNumTerritories(currentPlayer) + game.GetNumPlacedArmies(currentPlayer);

                scores.Add((playerScore, currentPlayer));
            }

            scores.Sort();

            foreach (var currentPlayer in players)
            {
                await sendGameOverRequest(currentPlayer, gameDuration, scores);
            }
        }

        private async Task sendGameOverRequest(ApiPlayer player, TimeSpan gameDuration, List<(int score, ApiPlayer player)> scores)
        {
            var gameOverRequest = new GameOverRequest {
                FinalBoard = game.Board.SerializableTerritories,
                GameDuration = gameDuration.ToString(),
                WinnerName = scores.Last().player.Name,
                FinalScores = scores.Select(s => $"{s.player.Name} ({s.score})")
            };

            var response = await (player.HttpClient.PostAsJsonAsync("/gameOver", gameOverRequest));
        }

        public bool IsAllArmiesPlaced()
        {

            int playersWithNoRemaining = game.Players.Where(p => game.GetPlayerRemainingArmies(p.Token) == 0).Count();

            if (playersWithNoRemaining == game.Players.Count())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void RemovePlayerFromBoard(String token)
        {
            foreach (Territory territory in game.Board.Territories)
            {
                if (territory.Owner == game.GetPlayer(token))
                {
                    territory.Owner = null;
                    territory.Armies = 0;
                }
            }
        }

        private async Task<ContinueAttackResponse> askContinueAttackingAsync(ApiPlayer currentPlayer, Territory attackingTerritory, Territory defendingTerritory)
        {
            var continueAttackingRequest = new ContinueAttackRequest {
                Board = game.Board.SerializableTerritories,
                AttackingTerritorry = attackingTerritory,
                DefendingTerritorry = defendingTerritory
            };
            var continueAttackingResponse = await (await currentPlayer.HttpClient.PostAsJsonAsync("/continueAttacking", continueAttackingRequest))
                .EnsureSuccessStatusCode()
                .Content.ReadFromJsonAsync<ContinueAttackResponse>();
            return continueAttackingResponse;
        }

        public void BootPlayerFromGame(ApiPlayer player)
        {
            RemovePlayerFromBoard(player.Token);
            players.Remove(player);
        }


    }
}
