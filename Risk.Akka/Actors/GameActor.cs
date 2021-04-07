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
        private List<string> names { get; set; }
        public GameActor(string secretCode, GameStartOptions startOptions)
        {
            this.secretCode = secretCode;
            game = new Game.Game(startOptions);
            names = new();
            Become(Starting);
        }

        public void Starting()
        {
            Receive<JoinGameMessage>(msg =>
            {
                var assignedName = AssignName(msg.RequestedName);
                names.Add(assignedName);
                game.Players.Add(msg.Actor);
                msg.Actor.Tell(new JoinGameResponse(assignedName));
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
                if (isNotCurrentPlayer(Sender))
                {
                    //send bad player actor an invalid request message
                    //send unable to deploy message to player
                    return;
                }
                if (game.TryPlaceArmy(Sender, msg.To))
                {
                    Sender.Tell(new ConfirmDeployMessage());
                }
            });
        }

        private bool isNotCurrentPlayer(IActorRef CurrentPlayer) => game.CurrentPlayer != CurrentPlayer;


        private string AssignName(string requestedName)
        {
            int sameNames = 2;
            var assignedPlayerName = requestedName;
            while (names.Contains(assignedPlayerName))
            {
                assignedPlayerName = string.Concat(requestedName, sameNames.ToString());
                sameNames++;
            }
            return assignedPlayerName;
        }


    }
}
