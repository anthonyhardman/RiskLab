using Akka.Actor;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using Risk.Game;
using System.Linq;
using Akka.Event;

namespace Risk.Akka.Actors
{
    public class GameActor : ReceiveActor
    {
        public ILoggingAdapter Log { get; } = Context.GetLogger();
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
                    Become(Deploying);
                    game.StartGame();
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

        public void Deploying()
        {
            Receive<JoinGameMessage>(_ =>
            {
                Sender.Tell(new UnableToJoinMessage());
            });

            Receive<DeployMessage>(msg =>
            {
                if (isCurrentPlayer(msg.Player) && game.TryPlaceArmy(msg.Player, msg.To))
                {
                    Sender.Tell(new ConfirmDeployMessage());
                    Log.Info($"{msg.Player} successfully deployed to {msg.To}");
                    var nextPlayer = game.NextPlayer();
                    if(game.GameState == GameState.Deploying)
                    {
                        Sender.Tell(new TellUserDeployMessage(nextPlayer, game.Board));
                    }
                    else
                    {
                        Become(Attacking);
                        Sender.Tell(new TellUserAttackMessage(nextPlayer, game.Board));
                    }
                }
                else
                {
                    msg.Player.Tell(new InvalidPlayerRequestMessage());
                    Sender.Tell(new BadDeployRequest(msg.Player));
                    Log.Info($"{msg.Player} failed to deploy to {msg.To}");
                }
                Sender.Tell(new GameStatusMessage(game.GetGameStatus()));
            });
        }

        public void Attacking()
        {

        }

        private bool isCurrentPlayer(IActorRef CurrentPlayer) => game.CurrentPlayer == CurrentPlayer;


        


    }
}
