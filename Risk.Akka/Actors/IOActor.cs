using Akka.Actor;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Risk.Akka.Actors
{
    public class IOActor : ReceiveActor
    {
        private readonly IRiskIOBridge riskIOBridge;
        Dictionary<IActorRef, string> players;
        private ActorSelection gameActor;
        private List<string> names { get; set; }

        public IOActor(IRiskIOBridge riskIOBridge)
        {
            this.riskIOBridge = riskIOBridge;
            names = new();
            gameActor = Context.ActorSelection(ActorNames.Path(ActorNames.Game));
            players = new Dictionary<IActorRef, string>();
            Become(Active);
        }

        public void Active()
        {
            Receive<SignupMessage>(msg =>
            {

                if (players.ContainsValue(msg.ConnectionId))
                {
                    riskIOBridge.JoinFailed(msg.ConnectionId);
                    return;
                }
                var assignedName = AssignName(msg.RequestedName);
                names.Add(assignedName);
                var newPlayer = Context.ActorOf(Props.Create(() => new PlayerActor(assignedName, msg.ConnectionId)), msg.ConnectionId);
                gameActor.Tell(new JoinGameMessage(assignedName, newPlayer));
                players.Add(newPlayer, msg.ConnectionId);
                riskIOBridge.JoinConfirmation(assignedName, msg.ConnectionId);
            });

            Receive<JoinGameResponse>(msg =>
            {
                riskIOBridge.JoinConfirmation(msg.AssignedName, players[Sender]);
            });

            Receive<UnableToJoinMessage>(msg =>
            {
                riskIOBridge.JoinFailed(players[Sender]);
                players.Remove(Sender);
            });

            Receive<BridgeDeployMessage>(msg =>
            {
                var player = players.FirstOrDefault(x => x.Value == msg.ConnectionId).Key;
                gameActor.Tell(new DeployMessage(msg.To, player));
            });

            Receive<BadDeployRequest>(msg =>
            {
                riskIOBridge.BadDeployRequest(players[msg.Player]);
            });

            Receive<BridgeAttackMessage>(msg =>
            {
                var player = players.FirstOrDefault(x => x.Value == msg.ConnectionId).Key;
                gameActor.Tell(new AttackMessage(msg.Defending, msg.Attacking, player));
            });

            Receive<BridgeCeaseAttackingMessage>(msg =>
            {
                var player = players.FirstOrDefault(x => x.Value == msg.ConnectionId).Key;
                gameActor.Tell(new CeaseAttackingMessage(player));
            });

            Receive<ConfirmDeployMessage>(msg =>
            {
                //riskIOBridge.
            });

            Receive<GameStatusMessage>(msg =>
            {
                riskIOBridge.SendGameStatus(msg.Status);
            });

            Receive<StartGameMessage>(msg =>
            {
                gameActor.Tell(msg);
            });

            Receive<GameStartingMessage>(msg =>
            {
                riskIOBridge.GameStarting();
            });

            Receive<TellUserDeployMessage>(msg =>
            {
                riskIOBridge.AskUserDeploy(players[msg.Player], msg.Board);
            });

            Receive<TellUserAttackMessage>(msg =>
            {
                riskIOBridge.AskUserAttack(players[msg.Player], msg.Board);
            });

            Receive<ChatMessage>(msg =>
            {
                riskIOBridge.SendChatMessage(players[msg.Player], msg.MessageText);
            });

            Receive<GameOverMessage>(msg =>
            {
                riskIOBridge.GameOver(msg.gameStatus);
            });

            Receive<TooManyInvalidRequestsMessage>(msg =>
            {
                riskIOBridge.SendChatMessage(players[msg.Player], "To many invalid requests, you've been kicked from the game.");
            });
        }

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
