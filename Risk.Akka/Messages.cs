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
    public record JoinGameMessage(string RequestedName, string ConnectionId);
    public record JoinGameResponse(string AssignedName, string ConnectionId);
    public record NoGameResponse;
    public record SignupMessage(string RequestedName, string ConnectionId);
    public record StartGameMessage(string SecretCode);
    public record UnableToJoinMessage;
    public record DeployMessage(Location to, string ConnectionId);
    public record ConfirmDeployMessage();
}
