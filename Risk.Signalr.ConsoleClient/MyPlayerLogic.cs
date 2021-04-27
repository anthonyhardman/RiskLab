using System.Collections.Generic;
using System.Linq;
using Risk.Shared;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

namespace Risk.Signalr.ConsoleClient
{
    public class MyPlayerLogic : HostedPlayerLogic
    {
        private int currentColumn = 0;
        public MyPlayerLogic(IConfiguration configuration, IHostApplicationLifetime appLifetime) : base(configuration, appLifetime)
        {

        }
        public override string MyPlayerName { get; set; } = "AnthonyBreanna";

        public override (Location from, Location to) WhereDoYouWantToAttack(IEnumerable<BoardTerritory> board)
        {
            var myFightableTerritories = board.Where(t => t.OwnerName == MyPlayerName && t.Armies > 1);

            foreach (var t in myFightableTerritories)
            {
                var neighbors = GetNeighbors(t, board);

                var availableNeighbor = neighbors.FirstOrDefault(t => t.OwnerName != MyPlayerName);

                if (availableNeighbor != null)
                {
                    return (t.Location, availableNeighbor.Location);
                }
            }

            // foreach (var myTerritory in board.Where(t => t.OwnerName == MyPlayerName).OrderByDescending(t => t.Armies))
            // {
            //     var myNeighbors = GetNeighbors(myTerritory, board);
            //     var destination = myNeighbors.Where(t => t.OwnerName != MyPlayerName).OrderBy(t => t.Armies).FirstOrDefault();
            //     if (destination != null)
            //     {
            //         return (myTerritory.Location, destination.Location);
            //     }
            // }

            throw new System.Exception("Unable to find somewhere to attack.");
        }

        public override Location WhereDoYouWantToDeploy(IEnumerable<BoardTerritory> board)
        {
            var availableSpaces = board.Where(t => t.OwnerName == null).OrderByDescending(t => t.Location.Row);
            var height = board.Max(t => t.Location.Row);
            var width = board.Max(t => t.Location.Column);
            var row = board.First();
           // var loc = new Location(height, 0);
            
            // if (currentColumn <= width)
            // {
            //     var loc = new Location(height, currentColumn++);
            //     return loc;
                
            // }
            
            return availableSpaces.First().Location;

        }
    }
}