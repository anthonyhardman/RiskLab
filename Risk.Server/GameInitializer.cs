using Akka.Actor;
using Risk.Server.Hubs;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Risk.Server
{
    public static class GameInitializer
    {
        public static ActorSystem InitializeGame(int height, int width, int numOfArmies, RiskHub riskHub, string secretCode)
        {
            GameStartOptions startOptions = new GameStartOptions
            {
                Height = height,
                Width = width,
                StartingArmiesPerPlayer = numOfArmies,
                
            };
            Game.Game newGame = new Game.Game(startOptions);

            var actorSystem = Risk.Akka.Startup.Init(secretCode, riskHub);
            return actorSystem;

        }
    }
}
