using Akka.Actor;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risk.Akka.Actors
{
    public class IOActor : ReceiveActor
    {
        private readonly IRiskIOBridge riskIOBridge;
        Dictionary<IActorRef, string> players;
        List<string> takenPlayerNames;
        public IOActor(IRiskIOBridge riskIOBridge)
        {
            this.riskIOBridge = riskIOBridge;
            players = new Dictionary<IActorRef, string>();
            takenPlayerNames = new List<string>();
            Become(Active);
        }

        public void Active()
        {
            Receive<SignupMessage>(msg =>
            {
                var assignedPlayerName = msg.RequestedName;

                if (players.ContainsValue(msg.ConnectionId))
                {
                    riskIOBridge.JoinFailed(msg.ConnectionId);
                    return;
                }
                if (takenPlayerNames.Contains(assignedPlayerName))
                {
                    assignedPlayerName = uniquePlayerName(assignedPlayerName);
                }
                var newPlayer = Context.ActorOf(Props.Create(() => new PlayerActor(assignedPlayerName)), ActorConstants.PlayerActorName);
                players.Add(newPlayer, msg.ConnectionId);
                Sender.Tell(new ConfirmPlayerSignup(assignedPlayerName));
            });

            Receive<UnableToJoinMessage>(msg =>
            {
                riskIOBridge.JoinFailed(players[Sender]);
                players.Remove(Sender);
            });
        }

        private string uniquePlayerName(string assignedPlayerName)
        {
            int sameNames = 0;
            while (takenPlayerNames.Contains(assignedPlayerName))
            {
                assignedPlayerName = string.Concat(assignedPlayerName, sameNames.ToString());
            }
            return assignedPlayerName;
        }
    }

}
