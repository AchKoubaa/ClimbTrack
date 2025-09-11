using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface ILocalHttpServer : IDisposable
    {
        string BaseUrl { get; }
        event EventHandler<HttpListenerContext> RequestReceived;
        Task StartAsync();
        void Stop();
    }
}
