using Akka.Actor;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risk.Akka.Actors
{
    public class PlayerActor : ReceiveActor
    {
        private readonly string connectionId;

        public string AssignedName { get; set; }
        public PlayerActor(string requestedName, string connectionId)
        {
            Context.ActorSelection(ActorConstants.GamePath).Tell(new JoinGameMessage(requestedName, connectionId));
            this.connectionId = connectionId; 
            Become(Joining);
        }

        public void Joining()
        {
            Receive<JoinGameResponse>(msg =>
            {
                AssignedName = msg.AssignedName;
                Context.Parent.Forward(msg);
                Become(Joined);
            });

            Receive<NoGameResponse>(msg =>
            {
                Context.Parent.Tell(new UnableToJoinMessage());
            });
        }

        public void Sleeping()
        {

        }

        public void Joined()
        {

        }
    }
}
