using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Risk.Shared
{
    public interface IRiskIOBridge
    {
        Task JoinFailed(string connectionId);
        Task JoinConfirmation(string assignedName, string connectionId);
    }
}
