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
        public GameActor(string secretCode)
        {
            this.secretCode = secretCode;
            game = new Game.Game();
            Become(Starting);
        }

        public void Starting()
        {
            Receive<JoinGameMessage>(msg =>
            {
                game.Players.Add(msg.Actor);
                game.AssignedNames.Add(msg.Actor, msg.AssignedName);
            });

            Receive((Action<StartGameMessage>)(msg =>
            {
                StartOrRestartGame(msg.SecretCode, msg.StartOptions, Sender);
            }));

            Receive<RestartGameMessage>(msg =>
            {
                StartOrRestartGame(msg.SecretCode, msg.StartOptions, Sender);
            });

            Receive<TooManyInvalidRequestsMessage>(msg => {
                game.RemovePlayerFromGame(msg.Player);
                Context.ActorSelection(ActorNames.Path(ActorNames.IO)).Tell(new TooManyInvalidRequestsMessage(msg.Player));
            });
        }

        private void StartOrRestartGame(string secretCode, GameStartOptions startOptions, IActorRef Sender)
        {
            if (this.secretCode == secretCode)
            {
                Become(Deploying);
                game.InitializeGame(startOptions);
                game.StartGame();
                Sender.Tell(new GameStartingMessage());
                Sender.Tell(new TellUserDeployMessage(game.CurrentPlayer, game.Board));
            }
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

            Receive<TooManyInvalidRequestsMessage>(msg => {
                game.RemovePlayerFromGame(msg.Player);
                Context.ActorSelection(ActorNames.Path(ActorNames.IO)).Tell(new TooManyInvalidRequestsMessage(msg.Player));
            });
        }

        public void Attacking()
        {
            Receive<CeaseAttackingMessage>(msg =>
            {
                if (isCurrentPlayer(msg.Player))
                {
                    Sender.Tell(new TellUserAttackMessage(game.NextPlayer(), game.Board));
                }
                else
                {
                    msg.Player.Tell(new InvalidPlayerRequestMessage());
                    Sender.Tell(new BadAttackRequest(msg.Player));
                }
            });

            Receive<AttackMessage>(msg =>
            {
                if (isCurrentPlayer(msg.Player))
                {
                    if(game.Players.Count <= 1 || game.Players.Any(p => game.PlayerCanAttack(p)) is false)
                    {
                        game.SetGameOver();
                        Log.Info("Ending Game. Player count = " + game.Players.Count + ";");
                        Sender.Tell(new GameOverMessage(game.GetGameStatus()));
                        Become(GameOver);
                        return;
                    }

                    if (game.PlayerCanAttack(msg.Player))
                    {
                        TryAttackResult attackResult = new TryAttackResult { AttackInvalid = false };
                        Territory attackingTerritory = null;
                        Territory defendingTerritory = null;
                        try
                        {
                            attackingTerritory = game.Board.GetTerritory(msg.Attacking);
                            defendingTerritory = game.Board.GetTerritory(msg.Defending);

                            Log.Info($"{msg.Player} wants to attack from {attackingTerritory} to {defendingTerritory}");

                            attackResult = game.TryAttack(msg.Player, attackingTerritory, defendingTerritory);
                            Sender.Tell(new GameStatusMessage(game.GetGameStatus()));
                        }
                        catch (Exception ex)
                        {
                            attackResult = new TryAttackResult { AttackInvalid = true, Message = ex.Message };
                        }
                        if (attackResult.AttackInvalid)
                        {
                            msg.Player.Tell(new InvalidPlayerRequestMessage());
                            Log.Error($"Invalid attack request! {msg.Player} from {attackingTerritory} to {defendingTerritory}.");
                            Sender.Tell(new ChatMessage(msg.Player, $"Invalid attack request: {attackResult.Message} :("));
                            Sender.Tell(new TellUserAttackMessage(msg.Player, game.Board));
                        }
                        else
                        {
                            Sender.Tell(new ChatMessage(msg.Player, $"Successfully Attacked From ({msg.Attacking.Row}, {msg.Attacking.Column}) To ({msg.Defending.Row}, {msg.Defending.Column})"));
                            if (game.GameState == GameState.Attacking)
                            {
                                if (game.PlayerCanAttack(msg.Player))
                                {
                                    Sender.Tell(new TellUserAttackMessage(msg.Player, game.Board));
                                }
                                else
                                    Sender.Tell(new TellUserAttackMessage(game.NextPlayer(), game.Board));
                            }
                            else
                            {
                                game.SetGameOver();
                                Sender.Tell(new GameOverMessage(game.GetGameStatus()));
                                Become(GameOver);
                            }
                        }
                    }
                    else
                    {
                        Log.Error("Player tried to attack when they couldn't attack");
                        Sender.Tell(new TellUserAttackMessage(game.NextPlayer(), game.Board));
                    }
                }
                else
                {
                    msg.Player.Tell(new InvalidPlayerRequestMessage());
                    Sender.Tell(new BadAttackRequest(msg.Player));
                }
            });

            Receive<TooManyInvalidRequestsMessage>(msg => {
                game.RemovePlayerFromGame(msg.Player);
                Context.ActorSelection(ActorNames.Path(ActorNames.IO)).Tell(new TooManyInvalidRequestsMessage(msg.Player));
            });
        }

        public void GameOver()
        {
            Receive<RestartGameMessage>(msg =>
            {
                if(msg.SecretCode == secretCode)
                {
                    Become(Starting);
                    Context.Self.Tell(new StartGameMessage(msg.SecretCode, msg.StartOptions), Context.Sender);
                }
            });
        }

        private bool isCurrentPlayer(IActorRef CurrentPlayer) => game.CurrentPlayer == CurrentPlayer;


        


    }
}
