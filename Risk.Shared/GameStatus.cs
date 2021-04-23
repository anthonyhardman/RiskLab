using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace Risk.Shared
{
    public class GameStatus
    {
        public IEnumerable<string> Players { get; set; }
        public Collection<PlayerStats> PlayerStats { get; set; }
        public GameState GameState { get; set; }
        public IEnumerable<BoardTerritory> Board { get; set; }
        public string CurrentPlayerName { get; set; }

        public GameStatus()
        {
            Players = new List<string>();
            PlayerStats = new Collection<PlayerStats>();
        }

        public GameStatus(IEnumerable<string> players, GameState gameState, IEnumerable<BoardTerritory> board, IEnumerable<PlayerStats> playerStats, string currentPlayerName )
        {
            Players = players;
            GameState = gameState;
            Board = board;
            CurrentPlayerName = currentPlayerName;
            PlayerStats = new Collection<PlayerStats>(playerStats.ToList());
        }
    }

    public class PlayerStats
    {
        public string Name { get; set; }
        public int Armies { get; set; }
        public int Territories { get; set; }
        public int Score { get; set; }
    }
}
