using System;
using System.Collections.Generic;
using System.Text;

namespace Risk.Shared
{
    public static class ActorNames
    {
        public const string Game = "GameActor";
        public const string IO = "IOActor";

        public static string Path(string actorName) => "akka://Risk/user/" + actorName;
    }
}
