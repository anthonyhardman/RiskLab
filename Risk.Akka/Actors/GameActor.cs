using Akka.Actor;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Risk.Game;
using System.Linq;

namespace Risk.Akka.Actors
{
    public class GameActor : ReceiveActor
    {
        private string secretCode { get; set; }
        private Risk.Game.Game game { get; set; }
        public GameActor(string secretCode, GameStartOptions startOptions)
        {
            this.secretCode = secretCode;
            game = new Game.Game(startOptions);
            Become(Starting);
        }

        public void Starting()
        {
            Receive<JoinGameMessage>(async msg =>
            {
                var assignedName = AssignName(msg.RequestedName);
                game.Players.Add(msg.Actor);
                Sender.Tell(new JoinGameResponse(assignedName, msg.ConnectionId));
            });

            Receive<StartGameMessage>(msg =>
            {
                if(secretCode == msg.SecretCode)
                {
                    Become(Running);
                    Sender.Tell(new GameStartingMessage());
                }
                else
                {
                    Sender.Tell(new CannotStartGameMessage());
                }
            });
        }

        public void Running()
        {
            Receive<JoinGameMessage>(_ =>
            {
                Sender.Tell(new UnableToJoinMessage());
            });

            Receive<GameDeployMessage>(msg =>
            {
                if (isNotCurrentPlayer(msg.ConnectionId))
                {
                    //send bad player actor an invalid request message
                    //send unable to deploy message to player
                    return;
                }
                if (game.TryPlaceArmy(msg.AssignedName, msg.to))
                {
                    Sender.Tell(new ConfirmDeployMessage());
                }
            });
        }

        private bool isNotCurrentPlayer(string connectionId) => game.CurrentPlayer.Token != connectionId;


        private string AssignName(string requestedName)
        {
            int sameNames = 2;
            var assignedPlayerName = requestedName;
            while (game.Players.Any(p => p.Name == assignedPlayerName))
            {
                assignedPlayerName = string.Concat(requestedName, sameNames.ToString());
                sameNames++;
            }
            return assignedPlayerName;
        }


    }
}
