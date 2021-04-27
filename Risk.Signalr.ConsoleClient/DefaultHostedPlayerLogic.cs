using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Risk.Shared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Risk.Signalr.ConsoleClient
{
    public class DefaultHostedPlayerLogic : HostedPlayerLogic
    {
        public DefaultHostedPlayerLogic(IConfiguration configuration, IHostApplicationLifetime applicationLifetime) : base(configuration, applicationLifetime)
        {

        }

        public override Location WhereDoYouWantToDeploy(IEnumerable<BoardTerritory> board)
        {
            var myTerritory = board.FirstOrDefault(t => t.OwnerName == MyPlayerName) ?? board.Skip(board.Count() / 2).First(t => t.OwnerName == null);
            return myTerritory.Location;
        }

        public override string MyPlayerName { get; set; } = "Default Player Logic";

        public override (Location from, Location to) WhereDoYouWantToAttack(IEnumerable<BoardTerritory> board)
        {
            foreach (var myTerritory in board.Where(t => t.OwnerName == MyPlayerName).OrderByDescending(t => t.Armies))
            {
                var myNeighbors = GetNeighbors(myTerritory, board);
                var destination = myNeighbors.Where(t => t.OwnerName != MyPlayerName).OrderBy(t => t.Armies).FirstOrDefault();
                if (destination != null)
                {
                    return (myTerritory.Location, destination.Location);
                }
            }
            throw new Exception("Unable to find place to attack");
        }
    }
}
