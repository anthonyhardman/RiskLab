using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Risk.Akka.Messages
{
    public class SignupMessage
    {
        public string ConnectionString { get; set; }
        public string RequestedName { get; set; }
    }
}
