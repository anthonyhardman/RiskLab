using System;
using System.Collections.Generic;
using System.Text;

namespace Risk.Shared
{
    public static class ActorNames
    {
        public const string Game = "GameActor";
        public const string IO = "IOActor";

        public static string Path(string root, string actorName) => $"{root}/user/{actorName}";
        public static string Path(string root, string parentName, string actorName) => $"{root}/user/{parentName}/{actorName}";
    }
}
