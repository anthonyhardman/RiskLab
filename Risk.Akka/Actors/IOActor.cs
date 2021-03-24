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
        Dictionary<IActorRef, string> players;
        public IOActor()
        {
            players = new Dictionary<IActorRef, string>();
            Receive<SignupMessage>(msg =>
            {
                var newPlayer = Context.ActorOf(Props.Create(() => new PlayerActor(msg.RequestedName)), ActorConstants.PlayerActorName);
                players.Add(newPlayer, msg.ConnectionString);
                Sender.Tell(new ConfirmPlayerSignup());
            });
        }
    }

}
