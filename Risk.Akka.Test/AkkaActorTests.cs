using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.TestKit.NUnit3;
using Moq;
using NUnit.Framework;
using Risk.Akka.Actors;
using Risk.Akka.Messages;
using Risk.Shared;

namespace Risk.Akka.Test
{
    class AkkaActorTests : TestKit
    {
        [SetUp]
        public void Setup()
        {
            riskIOBridgeMock = new Mock<IRiskIOBridge>();
        }

        Mock<IRiskIOBridge> riskIOBridgeMock;

        [Test]
        public void TestPlayerJoiningIOActor()
        {
            var signupMessage = new SignupMessage() { ConnectionString = "12345", RequestedName = "Test" };
            var IOActor = ActorOfAsTestActorRef(() => new IOActor(riskIOBridgeMock.Object), TestActor);
            IOActor.Tell(signupMessage);
            var confirmMessage = ExpectMsg<ConfirmPlayerSignup>();
            Assert.NotNull(confirmMessage);
        }

        [Test]
        public void TestPlayerUnableToJoinGame()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode"), TestActor);
            gameActor.Tell(new StartGameMessage() { SecretCode = "SecretCode" });
            ExpectMsg<GameStartingMessage>();

            gameActor.Tell(new JoinGameMessage() { RequestedName = "Test" });
            
            Assert.NotNull(ExpectMsg<UnableToJoinMessage>());
        }

        [Test]
        public void TestPlayerGameInteraction()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode"), TestActor);

            gameActor.Tell(new JoinGameMessage() { RequestedName = "Test" });
            
            Assert.NotNull(ExpectMsg<JoinGameResponse>());
        }

        [Test]
        public void BadPasswordAuthentication()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode"), TestActor);
            gameActor.Tell(new StartGameMessage() { SecretCode = "NotCorrect" });
            var badPasswordMessage = ExpectMsg<CannotStartGameMessage>();
            Assert.NotNull(badPasswordMessage);
        }

        [Test]
        public void SuccessfulPasswordAuthentication()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode"), TestActor);
            gameActor.Tell(new StartGameMessage() { SecretCode = "SecretCode" });
            var expectedMessage = ExpectMsg<GameStartingMessage>();
            Assert.NotNull(expectedMessage);
        }
    }
}
