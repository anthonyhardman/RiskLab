using System;
using System.Collections.Generic;
using System.Text;

namespace Risk.Shared
{
    public interface IRiskIOBridge
    {
        void JoinFailed(string connectionId);
    }
}
