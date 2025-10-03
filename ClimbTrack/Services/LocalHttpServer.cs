using Microcharts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class LocalHttpServer : ILocalHttpServer, IDisposable
    {
        private readonly HttpListener _listener;
        private readonly int _port;
        private readonly CancellationTokenSource _cts;
        private bool _isRunning;
        private bool _disposed;
        public bool IsRunning => _isRunning && _listener.IsListening;

       

        public async Task<bool> TestServerConnectionAsync()
        {
            if (!IsRunning)
                return false;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync($"{BaseUrl}test");
                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server connection test failed: {ex.Message}");
                return false;
            }
        }

        // Event to notify when a request is received
        public event EventHandler<HttpListenerContext> RequestReceived;

        public LocalHttpServer(int port = 3000)
        {
            if (!HttpListener.IsSupported)
            {
                throw new PlatformNotSupportedException("HttpListener is not supported on this platform");
            }

            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _cts = new CancellationTokenSource();
        }

        public string BaseUrl => $"http://localhost:{_port}/";

        public async Task StartAsync()
        {
            if (_isRunning)
            {
                return;
            }

            try
            {
                await Task.Yield(); // This makes the method truly async with minimal impact
                _listener.Start();
                _isRunning = true;
                
                Console.WriteLine($"HTTP server started on {BaseUrl}");

                // Start listening for requests
                _ = Task.Run(() => HandleRequestsAsync(_cts.Token));
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"Error starting HTTP listener: {ex.Message}");

                if (ex.ErrorCode == 5) // Access denied
                {
                    Console.WriteLine("Access denied. Try running the app with elevated privileges or use a port number above 1024.");
                }

                throw;
            }
        }

        private async Task HandleRequestsAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // GetContextAsync doesn't support cancellation directly
                        var getContextTask = Task.Run(() => _listener.GetContext());

                        // Wait for either the context or cancellation
                        if (await Task.WhenAny(getContextTask, Task.Delay(-1, cancellationToken)) == getContextTask)
                        {
                            var context = getContextTask.Result;
                            Console.WriteLine($"Request received: {context.Request.Url}");

                            // Raise the event
                            RequestReceived?.Invoke(this, context);

                            // If no event handlers, send a default response
                            if (RequestReceived == null)
                            {
                                SendDefaultResponse(context);
                            }
                        }
                        else
                        {
                            // Cancellation was requested
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error handling request: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error in request handler: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Request handler stopped");
            }
        }

        private void SendDefaultResponse(HttpListenerContext context)
        {
            try
            {
                string responseString = "<html><body><h1>Hello from MAUI!</h1><p>Your request has been processed.</p></body></html>";
                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                context.Response.ContentLength64 = buffer.Length;
                context.Response.ContentType = "text/html";
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                context.Response.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending default response: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            try
            {
                _cts.Cancel();

                if (_listener.IsListening)
                {
                    _listener.Stop();
                    Console.WriteLine("HTTP server stopped");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping HTTP server: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Stop();
                    _cts.Dispose();
                    _listener.Close();
                }

                _disposed = true;
            }
        }
    }
}
