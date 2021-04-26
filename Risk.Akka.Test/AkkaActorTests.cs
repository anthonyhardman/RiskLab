using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using Akka.TestKit.NUnit3;
using FluentAssertions;
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
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode"), TestActor);
            var playerActor = ActorOfAsTestActorRef(() => new PlayerActor("Test", "12345"), TestActor);
            gameActor.Tell(new StartGameMessage("SecretCode", startOptions));
            ExpectMsg<GameStartingMessage>();

            gameActor.Tell(new JoinGameMessage("Test", playerActor));
            
            Assert.NotNull(ExpectMsg<UnableToJoinMessage>());
        }

        [Test]
        public void TestPlayerGameInteraction()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode"), TestActor);
            var playerActor = ActorOfAsTestActorRef(() => new PlayerActor("Test", "12345"), TestActor);


            gameActor.Tell(new JoinGameMessage("Test", playerActor));
            
            Assert.NotNull(ExpectMsg<JoinGameResponse>());
        }

        [Test]
        public void BadPasswordAuthentication()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode"), TestActor);
            gameActor.Tell(new StartGameMessage("NotCorrect", startOptions));

            Assert.NotNull(ExpectMsg<CannotStartGameMessage>());
        }

        [Test]
        public void SuccessfulPasswordAuthentication()
        {
            var gameActor = ActorOfAsTestActorRef(() => new GameActor("SecretCode"), TestActor);
            gameActor.Tell(new StartGameMessage("SecretCode", startOptions));

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
            //Arrange
            var gameActor = Sys.ActorOf(Props.Create(() => new GameActor("banana55")));

            var signups = new List<(string assignedName, string connectionId)>();
            var mockBridge = new Mock<IRiskIOBridge>();
            mockBridge.Setup(m => m.JoinConfirmation(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string,string>((assignedName, connectionId) =>
                {
                    signups.Add((assignedName, connectionId));
                });

            var ioActor = Sys.ActorOf(Props.Create(() => new IOActor(mockBridge.Object)));

            //Act
            ioActor.Tell(new SignupMessage("Bogus", "12345"));
            ioActor.Tell(new SignupMessage("Bogus", "54321"));

            AwaitAssert(() =>
            {
                signups.Count.Should().Be(2);
                signups.First().assignedName.Should().Be("Bogus");
                signups.Skip(1).First().assignedName.Should().Be("Bogus2");
            });
        }

        [Test]
        public void StartGame()
        {
            //arrange
            var gameActor = Sys.ActorOf(Props.Create(() => new GameActor("banana55")), ActorNames.Game);
            var mockBridge = new Mock<IRiskIOBridge>();
            string connectionIdOfFirstPlayer = null;
            mockBridge.Setup(m => m.AskUserDeploy(It.IsAny<string>(), It.IsAny<Board>()))
                .Callback<string, Board>((connectionId, board) =>
                {
                    connectionIdOfFirstPlayer = connectionId;
                });
            var ioActor = Sys.ActorOf(Props.Create(() => new IOActor(mockBridge.Object)), ActorNames.IO);

            ioActor.Tell(new SignupMessage("Bogus", "12345"));
            ioActor.Tell(new SignupMessage("Bogus", "54321"));

            //act
            ioActor.Tell(new StartGameMessage("banana55", new GameStartOptions { ArmiesDeployedPerTurn = 5, Height = 5, Width = 5, StartingArmiesPerPlayer = 10 }));

            //Assert
            AwaitAssert(() =>
            {
                connectionIdOfFirstPlayer.Should().BeOneOf("12345", "54321");
            }, duration: TimeSpan.FromSeconds(1));
        }

    }
}
