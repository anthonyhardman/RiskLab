using Akka.Actor;
using Akka.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Risk.Akka.Actors;
using Risk.Shared;

namespace Risk.Akka
{
    public static class Startup
    {
        public static ActorSystem Init(string secretCode)
        {
            var hocon = File.ReadAllText("risk.akka.hocon");
            var config = ConfigurationFactory.ParseString(hocon
                .Replace("{{HostPort}}", Constants.AdditionActorPort)
                .Replace("{{HostIp}}", Dns.GetHostName())
                .Replace("{{SystemName}}", Constants.ActorSystemName)
                .Replace("{{SeedIp}}", Dns.GetHostName())
                .Replace("{{SeedPort}}", Constants.SeedPort)
                );
            var actorSystem = ActorSystem.Create("Risk", config);
            actorSystem.ActorOf<IOActor>(ActorConstants.IOActor);
            actorSystem.ActorOf(() => GameActor(secretCode), ActorConstants.GameActor);
        }
    }
}
