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
        public const int MaxInvalidRequests = 5;
        private readonly string connectionId;
        public int invalidRequests { get; set; }

        public string AssignedName { get; set; }
        public PlayerActor(string assignedName, string connectionId)
        {
            this.AssignedName = assignedName;
            this.connectionId = connectionId;
            this.invalidRequests = 0;
            Become(Joined);
        }

        public void Joined()
        {
            Receive<InvalidPlayerRequestMessage>(msg =>
            {
                this.invalidRequests += 1;
                if(invalidRequests > MaxInvalidRequests)
                {
                    Sender.Tell(new TooManyInvalidRequestsMessage(Context.Self));
                }
            });
        }
    }
}
