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
        public IOActor(IRiskIOBridge riskIOBridge)
        {
            this.riskIOBridge = riskIOBridge;
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
                var newPlayer = Context.ActorOf(Props.Create(() => new PlayerActor(msg.RequestedName, msg.ConnectionId)), msg.ConnectionId);
                players.Add(newPlayer, msg.ConnectionId);
                gameActor.Tell(new JoinGameMessage(msg.RequestedName, newPlayer));
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

            Receive<DeployMessage>(msg =>
            {
                var deployedPlayer = players.FirstOrDefault(x => x.Value == msg.ConnectionId).Key;
                deployedPlayer.Tell(msg);
            });

            Receive<ConfirmDeployMessage>(msg =>
            {
                //riskIOBridge.
            });
        }

        
    }

}
