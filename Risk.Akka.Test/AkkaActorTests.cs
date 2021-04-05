using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Akka.TestKit.NUnit3;
using Moq;
using NUnit.Framework;
using Risk.Akka.Actors;
using Risk.Shared;

namespace Risk.Akka.Test
{
    class AkkaActorTests : TestKit
    {
        [SetUp]
        public void Setup()
        {
            riskIOBridgeMock = new Mock<IRiskIOBridge>();
            startOptions = new GameStartOptions() { Height = 3, StartingArmiesPerPlayer = 3, Width = 3 };
        }
        GameStartOptions startOptions;
        Mock<IRiskIOBridge> riskIOBridgeMock;

        [Test]
        public void TestPlayerJoiningIOActor()
        {
            var signupMessage = new SignupMessage("Test", "12345");
            var IOActor = ActorOfAsTestActorRef(() => new IOActor(riskIOBridgeMock.Object), TestActor);

            IOActor.Tell(signupMessage);
            
            Assert.NotNull(ExpectMsg<ConfirmPlayerSignup>());
        }

        [Test]
        public void TestPlayerUnableToJoinGame()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode", startOptions), TestActor);
            gameActor.Tell(new StartGameMessage("SecretCode"));
            ExpectMsg<GameStartingMessage>();

            gameActor.Tell(new JoinGameMessage("Test", "12345"));
            
            Assert.NotNull(ExpectMsg<UnableToJoinMessage>());
        }

        [Test]
        public void TestPlayerGameInteraction()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode", startOptions), TestActor);

            gameActor.Tell(new JoinGameMessage("Test", "12345"));
            
            Assert.NotNull(ExpectMsg<JoinGameResponse>());
        }

        [Test]
        public void BadPasswordAuthentication()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode", startOptions), TestActor);
            gameActor.Tell(new StartGameMessage("NotCorrect"));

            Assert.NotNull(ExpectMsg<CannotStartGameMessage>());
        }

        [Test]
        public void SuccessfulPasswordAuthentication()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode", startOptions), TestActor);
            gameActor.Tell(new StartGameMessage("SecretCode"));

            Assert.NotNull(ExpectMsg<GameStartingMessage>());
        }

        [Test]
        public void UniqueConnectionId()
        {
            var IOActor = ActorOfAsTestActorRef(() => new IOActor(riskIOBridgeMock.Object), TestActor);

            IOActor.Tell(new SignupMessage("Test", "12345"));
            ExpectMsg<ConfirmPlayerSignup>();

            IOActor.Tell(new SignupMessage("Test2", "12345"));
            Assert.NotNull(ExpectMsg<UnableToJoinMessage>());
        }

        [Test]
        public void UniquePlayerName()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode", startOptions), TestActor);

            gameActor.Tell(new JoinGameMessage("Test", "12345"));
            ExpectMsg<JoinGameResponse>();
            
            gameActor.Tell(new JoinGameMessage("Test", "unique"));
            Assert.AreEqual("Test0", ExpectMsg<JoinGameResponse>().AssignedName);
        }

        [Test]
        public void DeployMessageTest()
        {

        }

    }
}
