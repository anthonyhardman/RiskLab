using Akka.Actor;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using static Risk.Shared.ActorNames;

namespace Risk.Akka.Actors
{
    public class PlayerActor : ReceiveActor
    {
        private readonly string connectionId;
        public int invalidRequests { get; set; }

        public string AssignedName { get; set; }
        public PlayerActor(string assignedName, string connectionId)
        {
            this.AssignedName = assignedName;
            this.connectionId = connectionId;
            this.invalidRequests = 0;
            Become(Joining);
        }

        public void Joining()
        {
            Receive<JoinGameResponse>(msg =>
            {
                Context.Parent.Tell(new TellUserJoinedMessage(AssignedName, connectionId));
                Become(Joined);
            });

            Receive<NoGameResponse>(msg =>
            {
                Context.Parent.Tell(new UnableToJoinMessage());
            });

            Receive<InvalidPlayerRequestMessage>(msg =>
            {
                this.invalidRequests += 1;
            });
        }

        public void Sleeping()
        {

        }

        public void Joined()
        {
            Receive<DeployMessage>(msg =>
            {
                Context.ActorSelection(Path(ActorNames.Game)).Tell(new GameDeployMessage(msg.To, AssignedName));
            });

            Receive<ConfirmDeployMessage>(msg =>
            {
                Context.Parent.Forward(msg);
            });

            Receive<InvalidPlayerRequestMessage>(msg =>
            {
                this.invalidRequests += 1;
            });

        }
    }
}
