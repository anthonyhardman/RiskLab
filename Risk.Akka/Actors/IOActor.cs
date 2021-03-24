using Akka.Actor;
using Risk.Akka.Messages;
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
        public IOActor(IRiskIOBridge riskIOBridge)
        {
            this.riskIOBridge = riskIOBridge;
            players = new Dictionary<IActorRef, string>();
            Become(Active);
        }

        public void Active()
        {
            Receive<SignupMessage>(msg =>
            {
                var newPlayer = Context.ActorOf(Props.Create(() => new PlayerActor(msg.RequestedName)), ActorConstants.PlayerActorName);
                players.Add(newPlayer, msg.ConnectionString);
                Sender.Tell(new ConfirmPlayerSignup());
            });

            Receive<UnableToJoinMessage>(msg =>
            {
                riskIOBridge.JoinFailed(players[Sender]);
                players.Remove(Sender);
            });
        }
    }

}
