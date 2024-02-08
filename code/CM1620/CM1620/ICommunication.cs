using System.Collections.Generic;
using CM1620.Models;

namespace CM1620
{
    public interface ICommunication : IDisposable
    {
        Task<IsdtResponse> SendCommand(string command, string? args = null);
    }
}