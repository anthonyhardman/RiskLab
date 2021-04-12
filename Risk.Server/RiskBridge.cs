using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Risk.Server.Hubs;
using Risk.Shared;
using System;
using System.Threading.Tasks;

namespace Risk.Server
{
    public class RiskBridge : IRiskIOBridge
    {
        private RiskHub riskHub => services.GetService<RiskHub>();
        private ILogger<RiskBridge> logger => services.GetService<ILogger<RiskBridge>>();
        private readonly IServiceProvider services;

        public RiskBridge(IServiceProvider services)
        {
            this.services = services;
            
        }
        public async Task JoinConfirmation(string assignedName, string connectionId)
        {
            await riskHub.JoinConfirmation(assignedName, connectionId);
        }

        public async Task JoinFailed(string connectionId)
        {
            await riskHub.JoinFailed(connectionId);
        }

        public async Task ConfirmDeploy(string connectionId)
        {
            await riskHub.ConfirmDeploy(connectionId);
        }

        public async Task GameStarting()
        {
            await riskHub.AnnounceStartGame();
        }

        public async Task AskUserDeploy(string connectionId, Board board)
        {
            await riskHub.AskUserDeploy(connectionId, board);
        }

        public async Task BadDeployRequest(string connectionId)
        {
            await riskHub.SendMessage(connectionId, "It's not your turn");
            logger.LogInformation($"{connectionId} tried to deploy when it wasn't their turn. Increasing invalid request count.");
        }
    }
}
