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
using System.Net;

namespace Risk.Akka
{
    public static class Startup
    {
        public static ActorSystem Init(string secretCode, IRiskIOBridge riskIOBridge)
        {
            var hocon = File.ReadAllText("risk.akka.hocon");
            var config = ConfigurationFactory.ParseString(hocon);
            var actorSystem = ActorSystem.Create("Risk", config);
            actorSystem.ActorOf(Props.Create(() => new IOActor(riskIOBridge)), ActorConstants.IOActorName);
            actorSystem.ActorOf(Props.Create(() => new GameActor(secretCode)), ActorConstants.GameActorName);
            return actorSystem;
        }
    }
}
