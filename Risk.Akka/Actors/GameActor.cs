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
            Receive<JoinGameMessage>(msg =>
            {
                game.Players.Add(msg.Actor);
                game.AssignedNames.Add(msg.Actor, msg.AssignedName);
            });

            Receive<PlayerStartingGameMessage>(msg =>
            {
                if(secretCode == msg.SecretCode)
                {
                    Become(Running);
                    Sender.Tell(new GameStartingMessage());
                    Sender.Tell(new TellUserDeployMessage(game.CurrentPlayer, game.Board));
                }
                else
                {
                    msg.Actor.Tell(new InvalidPlayerRequestMessage());
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


        


    }
}
