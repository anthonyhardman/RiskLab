﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Risk.Shared;

namespace Risk.Game
{
    public class Game
    {
        public Game(GameStartOptions startOptions)
        {
            Board = new Board(createTerritories(startOptions.Height, startOptions.Width));
            StartingArmies = startOptions.StartingArmiesPerPlayer;
            gameState = GameState.Initializing;
        }
        public IActorRef CurrentPlayer { get; set; }
        public List<IActorRef> Players { get; } = new List<IActorRef>();

        public int numberOfCardTurnIns = 1;
        

        public Board Board { get; }
        private GameState gameState { get; set; }

        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int StartingArmies { get; }
        public GameState GameState => gameState;

        public const int MaxTimesAPlayerCanNotAttack = 5;

        private IEnumerable<Territory> createTerritories(int height, int width)
        {
            var territories = new List<Territory>();
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    territories.Add(new Territory(new Location(r, c)));
                }
            }
            return territories;
        }

        public void StartJoining()
        {
            gameState = GameState.Joining;
        }

        public void StartGame()
        {
            Players.Shuffle();
            gameState = GameState.Deploying;
        }

        public bool TryPlaceArmy(string playerToken, Location desiredLocation)
        {
            bool placeResult;

            if (gameState != Shared.GameState.Deploying)
                return false;

            if (GetPlayerRemainingArmies(playerToken) < 1)
                return false;
            Territory territory;
            try
            {
                territory = Board.GetTerritory(desiredLocation);
            }
            catch { return false; }

            if (territory.Owner == null)
            {
                territory.Owner = GetPlayer(playerToken);
                territory.Armies = 1;
                placeResult = true;
            }
            else if (territory.Owner.Token != playerToken)
            {
                placeResult = false;
            }
            else //owner token == playerToken
            {
                if (GetPlayerRemainingArmies(playerToken) > 0)
                {
                    territory.Armies++;
                    placeResult = true;
                }
                else
                {
                    placeResult = false;
                }
            }

            if (placeResult && CanChangeToAttackState())
                gameState = GameState.Attacking;

            return placeResult;
        }

        public void RemovePlayerFromBoard(String token)
        {
            foreach (Territory territory in Board.Territories)
            {
                if (territory.Owner == GetPlayer(token))
                {
                    territory.Owner = null;
                    territory.Armies = 0;
                }
            }
        }

        public int GetPlayerRemainingArmies(string playerToken)
        {
            var player = GetPlayer(playerToken);
            var armiesOnBoard = GetNumPlacedArmies(player);
            return StartingArmies - armiesOnBoard;
        }

        public IPlayer GetPlayer(string token) => playerDictionary[token];

        public bool CanChangeToAttackState()
        {
            int totalRemainingArmies = playerDictionary.Values.Sum(p => GetPlayerRemainingArmies(p.Token));
            return totalRemainingArmies == 0;
        }

        public bool EnoughArmiesToAttack(Territory attacker)
        {
            return attacker.Armies > 1;
        }

        public bool AttackOwnershipValid(string playerToken, Location from, Location to)
        {
            var territoryFrom = Board.Territories.Single(t => t.Location == from);
            var territoryTo = Board.Territories.Single(t => t.Location == to);
            var player = GetPlayer(playerToken);
            return territoryFrom.Owner == player && territoryTo.Owner != player;
        }

        public bool PlayerCanAttack(IPlayer player)
        {
            foreach (var territory in Board.Territories.Where(t => t.Owner == player && EnoughArmiesToAttack(t)))
            {
                var neighbors = Board.GetNeighbors(territory);
                return neighbors.Any(n => n.Owner != player);
            }
            return false;
        }

        public GameStatus GetGameStatus()
        {
            var playerNames = from p in playerDictionary.Values
                              select p.Name;

            var playerStats = from p in playerDictionary.Values
                              let territories = Board.Territories.Where(t => t.Owner == p)
                              let armies = territories.Sum(t => t.Armies)
                              let territoryCount = territories.Count()
                              select new PlayerStats {
                                  Name = p.Name,
                                  Armies = armies,
                                  Territories = territoryCount,
                                  Score = armies + (territoryCount * 2)
                              };

            return new GameStatus(playerNames, GameState, Board.AsBoardTerritoryList(), playerStats);
        }

        public int GetNumPlacedArmies(IPlayer player)
        {
            return Board.Territories
                        .Where(t => t.Owner == player)
                        .Sum(t => t.Armies);
        }

        public const int MAX_ATTACKER_DICE = 3;
        public const int MAX_DEFENDER_DICE = 2;

        public void SetGameOver()
        {
            gameState = GameState.GameOver;
        }

        public TryAttackResult TryAttack(string attackerToken, Territory attackingTerritory, Territory defendingTerritory, int seed = 0)
        {
            if (canAttack(attackerToken, attackingTerritory, defendingTerritory) is false)
            {
                return new TryAttackResult { AttackInvalid = true };
            }

            Random rand;
            if (seed == 0)
            {
                rand = new Random();
            }
            else
            {
                rand = new Random(seed);
            }

            int[] attackerDice = new int[MAX_ATTACKER_DICE];
            int[] defenderDice = new int[MAX_DEFENDER_DICE];

            for (int i = 0; i < Math.Min(attackingTerritory.Armies, MAX_ATTACKER_DICE) - 1; i++)
            {
                attackerDice[i] = rand.Next(1, 7);
            }
            for (int i = 0; i < Math.Min(defendingTerritory.Armies, MAX_DEFENDER_DICE); i++)
            {
                defenderDice[i] = rand.Next(1, 7);
            }
            Array.Sort(attackerDice);
            Array.Sort(defenderDice);
            Array.Reverse(attackerDice);
            Array.Reverse(defenderDice);
            for (int i = 0; i <= defendingTerritory.Armies && i < defenderDice.Length && i < attackingTerritory.Armies - 1; i++)
            {
                if (attackerDice[i] > defenderDice[i])
                    defendingTerritory.Armies--;
                else
                    attackingTerritory.Armies--;
            }
            if (defendingTerritory.Armies < 1)
            {
                BattleWasWon(attackingTerritory, defendingTerritory);
                //AddOneTerritoryCard(attackingTerritory.Owner.TerritoryCards, defendingTerritory); // This is the Call to AddOneTerritoryCard()
                return new TryAttackResult {
                    CanContinue = false,
                    AttackInvalid = false
                };
            }
            return new TryAttackResult { CanContinue = attackingTerritory.Armies > 1, AttackInvalid = false };
        }
        //This adds one Territory card with a Random integer value between 1 and 3. The call is located in the tryAttack win Condition. This also checks that the length of the territory card hand is less than 6.
        public void AddOneTerritoryCard(List<int> territoryCards, Territory defendingTerritory)
        {
            Random rnd = new Random();
            int cardNumber = rnd.Next(1, 4);
            if (territoryCards.Count < 6)
            {
                territoryCards.Add(cardNumber);
            }

            if(territoryCards.Count > 2)
            {
                CheckCards(territoryCards, defendingTerritory);
            }
        }

        //This Function Checks the deck for one of each card, or three of a kind.
        public void CheckCards(List<int> Cards, Territory territory)
        {
            Cards.Sort();
            for (int x = 0; x < Cards.Count - 2; x++)
            {
                if (Cards[x] == Cards[x + 1] && Cards[x + 2] == Cards[x + 1])
                {
                    territory.Armies += (numberOfCardTurnIns * 5);
                    numberOfCardTurnIns++;
                    Cards.Remove(Cards[x + 2]);
                    Cards.Remove(Cards[x + 1]);
                    Cards.Remove(Cards[x]);
                }
                else if (Cards[x] + 1 == Cards[x + 1] && Cards[x + 1] + 1 == Cards[x + 2])
                {
                    territory.Armies += (numberOfCardTurnIns * 5);
                    numberOfCardTurnIns++;
                    Cards.Remove(Cards[x + 2]);
                    Cards.Remove(Cards[x + 1]);
                    Cards.Remove(Cards[x]);
                }
            }
        }

        private bool canAttack(string attackerToken, Territory attackingTerritory, Territory defendingTerritory)
        {
            return AttackOwnershipValid(attackerToken, attackingTerritory.Location, defendingTerritory.Location)
                 && EnoughArmiesToAttack(attackingTerritory)
                 && Board.AttackTargetLocationIsValid(attackingTerritory.Location, defendingTerritory.Location);
        }

        public int GetNumTerritories(IPlayer player) => Board.Territories.Count(t => t.Owner == player);

        public void BattleWasWon(Territory attackingTerritory, Territory defendingTerritory)
        {
            defendingTerritory.Owner = attackingTerritory.Owner;
            defendingTerritory.Armies = attackingTerritory.Armies - 1;
            attackingTerritory.Armies -= defendingTerritory.Armies;
        }
    }
}