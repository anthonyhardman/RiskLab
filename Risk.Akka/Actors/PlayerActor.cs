using Akka.Actor;
using Risk.Akka.Messages;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risk.Akka.Actors
{
    public class PlayerActor : ReceiveActor
    {
        public string AssignedName { get; set; }
        public PlayerActor(string requestedName)
        {
            Context.ActorSelection(ActorConstants.GamePath).Tell(new JoinGameMessage { RequestedName = requestedName });
            Become(Joining);
        }

        public void Joining()
        {
            Receive<JoinGameResponse>(msg =>
            {
                AssignedName = msg.GivenName;
            });

            Receive<NoGameResponse>(msg =>
            {
                Become(Sleeping);
            });
        }

        public void Sleeping()
        {

        }
    }
}
