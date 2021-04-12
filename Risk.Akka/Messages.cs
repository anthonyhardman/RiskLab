using Akka.Actor;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Risk.Akka
{
    public record CannotStartGameMessage;
    public record ConfirmPlayerSignup(string AssignedName);
    public record GameStartingMessage;
    public record JoinGameMessage(string AssignedName, IActorRef Actor);
    public record JoinGameResponse(string AssignedName);
    public record TellUserJoinedMessage(string AssignedName, string ConnectionId);
    public record NoGameResponse;
    public record SignupMessage(string RequestedName, string ConnectionId);
    public record StartGameMessage(string SecretCode, string ConnectionId);
    public record PlayerStartingGameMessage(string SecretCode, IActorRef Actor);
    public record UnableToJoinMessage;
    public record DeployMessage(Location To, string ConnectionId);
    public record GameDeployMessage(Location To, string ConnectionId);
    public record ConfirmDeployMessage();
    public record InvalidPlayerRequestMessage;
}
