using Akka.Actor;
using Risk.Akka.Actors;
using Risk.Server.Hubs;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risk.Server
{
    public static class GameInitializer
    {
        public static ActorSystem InitializeGame(int height, int width, int numOfArmies, string secretCode, RiskBridge riskBridge)
        {
            GameStartOptions startOptions = new GameStartOptions
            {
                Height = height,
                Width = width,
                StartingArmiesPerPlayer = numOfArmies,
                
            };
            Game.Game newGame = new Game.Game(startOptions);

            var actorSystem = Risk.Akka.Startup.Init(secretCode, riskBridge, startOptions);
            return actorSystem;
        }


    }
}
