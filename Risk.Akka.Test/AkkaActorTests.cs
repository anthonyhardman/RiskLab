using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.TestKit.NUnit3;
using NUnit.Framework;
using Risk.Akka.Actors;
using Risk.Akka.Messages;
using Risk.Shared;

namespace Risk.Akka.Test
{
    class AkkaActorTests : TestKit
    {
        [Test]
        public void TestPlayerJoiningIOActor()
        {
            var signupMessage = new SignupMessage() { ConnectionString = "12345", RequestedName = "Test" };
            var IOActor = ActorOfAsTestActorRef(() => new IOActor(), TestActor);
            IOActor.Tell(signupMessage);
            var confirmMessage = ExpectMsg<ConfirmPlayerSignup>();
            Assert.NotNull(confirmMessage);
        }

        [Test]
        public void TestPlayerGameInteraction()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor(), TestActor);
            gameActor.Tell(new JoinGameMessage() { RequestedName = "Test" });
            var confirmedJoinMessage = ExpectMsg<JoinGameResponse>();
            Assert.NotNull(confirmedJoinMessage);
        }

        [Test]
        public void BadPasswordAuthentication()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor(), TestActor);
            gameActor.Tell(new StartGameMessage() { Password = "NotCorrect" });
            var badPasswordMessage = ExpectMsg<CannotStartGameMessage>();
            Assert.NotNull(badPasswordMessage);
        }

        [Test]
        public void SuccessfulPasswordAuthentication()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor(), TestActor);
            gameActor.Tell(new StartGameMessage() { Password = ActorConstants.GamePassword });
            var expectedMessage = ExpectMsg<GameStartingMessage>();
            Assert.NotNull(expectedMessage);
        }
    }
}
