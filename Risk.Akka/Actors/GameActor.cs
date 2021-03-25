using Akka.Actor;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risk.Akka.Actors
{
    public class GameActor : ReceiveActor
    {
        public string SecretCode { get; set; }
        public GameActor(string secretCode )
        {
            SecretCode = secretCode;
            Become(Starting);
        }

        public void Starting()
        {
            Receive<JoinGameMessage>(msg =>
            {
                //update logic to finalize name later
                var finalizedName = msg.RequestedName;
                Sender.Tell(new JoinGameResponse(finalizedName));
            });

            Receive<StartGameMessage>(msg =>
            {
                if(SecretCode == msg.SecretCode)
                {
                    Become(Running);
                    Sender.Tell(new GameStartingMessage());
                }
                else
                {
                    Sender.Tell(new CannotStartGameMessage());
                }
            });

            
        }

        public void Running()
        {
            Receive<JoinGameMessage>(_ =>
            {
                Sender.Tell(new UnableToJoinMessage());
            });
        }
    }
}
