using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Risk.Server.Hubs;
using Risk.Shared;
using System;
using System.Threading.Tasks;

namespace Risk.Server
{
    public class RiskBridge : IRiskIOBridge
    {
        private RiskHub riskHub => services.GetService<RiskHub>();
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
    }
}
