using Firebase.Auth;
using System.Diagnostics;
using System.Net;
using System.Web;

namespace ClimbTrack.Services
{

    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly string GoogleClientId;
        private readonly string GoogleRedirectUri;
        private readonly ILocalHttpServer _httpServer;
        private readonly IFirebaseService _firebaseService;
        private TaskCompletionSource<string> _authCompletionSource;

        public GoogleAuthService(ILocalHttpServer httpServer, 
            IFirebaseService firebaseService
                                )
        {
            _httpServer = httpServer;
            _firebaseService = firebaseService;

            // Use your actual client ID from Google Cloud Console
            GoogleClientId = "703559950083-kbfeqqi9evvmmgbn482ff65iaq5tchud.apps.googleusercontent.com";

            // For Android, use the Android client ID specifically
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Replace with your Android client ID (ends with .apps.googleusercontent.com)
                GoogleClientId = "703559950083-android-specific-id.apps.googleusercontent.com";
            }
            else if (DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // Replace with your iOS client ID
                GoogleClientId = "703559950083-ios-specific-id.apps.googleusercontent.com";
            }

            Console.WriteLine($"Initialized GoogleAuthService with client ID: {GoogleClientId}");
        }

        public async Task<UserCredential> SignInWithGoogleAsync()
        {
            try
            {
                Console.WriteLine("Starting Google Sign-In process");
                Console.WriteLine($"Platform: {DeviceInfo.Platform}");
                Console.WriteLine($"Package name: {AppInfo.PackageName}");
                Console.WriteLine($"Google Client ID: {GoogleClientId.Substring(0, 10)}...");
                Console.WriteLine($"Redirect URI: {GoogleRedirectUri}");
              
                // For mobile platforms, we'll use a different approach
                if (DeviceInfo.Platform == DevicePlatform.Android )
                {
                    return await SignInWithGoogleOnMobile();
                }
                else if (DeviceInfo.Platform == DevicePlatform.iOS)
                {
                    //return await SignInWithGoogleOnIOS();
                    return await SignInWithGoogleOnMobile();
                }
                else
                {
                    // For desktop platforms, use a different approach
                    return await SignInWithGoogleOnDesktop();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Google authentication error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        private async Task<UserCredential> SignInWithGoogleOnMobile()
        {
            Console.WriteLine("Using mobile authentication flow with WebAuthenticator");

            // Define a callback URL that matches the intent filter in WebAuthenticatorCallbackActivity
            var callbackUrl = "http://localhost:3000";

            var authOptions = new WebAuthenticatorOptions
            {
                Url = new Uri($"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={GoogleClientId}&" +
               $"redirect_uri={Uri.EscapeDataString(GoogleRedirectUri)}&" +
               $"response_type=token&" +
               $"scope={Uri.EscapeDataString("openid profile email")}"
           ),
                CallbackUrl = new Uri(callbackUrl),
                PrefersEphemeralWebBrowserSession = false
            };

            try
            {
                var result = await WebAuthenticator.AuthenticateAsync(authOptions);

                if (result.Properties.TryGetValue("access_token", out var accessToken))
                {
                    Console.WriteLine("Access token obtained. Signing in with Firebase...");
                    return await _firebaseService.SignInWithGoogleAccessTokenAsync(accessToken);
                }
                else
                {
                    throw new Exception("No access token found in authentication result");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebAuthenticator error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<UserCredential> SignInWithGoogleOnDesktop()
        {
            Console.WriteLine("Using Windows-specific authentication flow");

            // Implementazione alternativa per Windows
            // Opzione 1: Usa un server locale per gestire il callback
            return await SignInWithGoogleUsingLocalServerAsync();

            // Opzione 2: Usa un approccio di autenticazione simulata per lo sviluppo
            // return await SignInWithGoogleMockAsync();
        }

        private async Task<UserCredential> SignInWithGoogleUsingLocalServerAsync()
        {
            _authCompletionSource = new TaskCompletionSource<string>();

            // Subscribe to the RequestReceived event
            _httpServer.RequestReceived += OnHttpRequestReceived;

            try
            {
                // Start the HTTP server
                await _httpServer.StartAsync();

                Console.WriteLine($"Started local HTTP server on {_httpServer.BaseUrl}");

                // Generate Google authentication URL
                var state = Guid.NewGuid().ToString("N");
                var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={GoogleClientId}&" +
                    $"redirect_uri={_httpServer.BaseUrl}&" +
                    $"response_type=token&" +
                    $"state={state}&" +
                    $"scope={Uri.EscapeDataString("openid profile email")}";

                // Open browser with authentication URL
                Process.Start(new ProcessStartInfo
                {
                    FileName = authUrl,
                    UseShellExecute = true
                });

                Console.WriteLine($"Opened browser with auth URL: {authUrl}");

                // Wait for the authentication to complete
                var accessToken = await _authCompletionSource.Task;

                if (!string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("Access token obtained. Signing in with Firebase...");
                    return await _firebaseService.SignInWithGoogleAccessTokenAsync(accessToken);
                }
                else
                {
                    throw new Exception("No access token found in authentication result");
                }
            }
            finally
            {
                // Clean up
                _httpServer.RequestReceived -= OnHttpRequestReceived;
                _httpServer.Stop();
            }
        }
        private void SendAuthCompletedResponse(HttpListenerContext context)
        {
            var response = context.Response;
            // This JavaScript extracts the token from the URL fragment and sends it back to the server
            var responseString = @"
    <html>
    <body>
        <h1>Authentication completed</h1>
        <p>Processing authentication response...</p>
        <script>
            // Extract the token from the URL fragment
            const hash = window.location.hash.substring(1);
            const params = new URLSearchParams(hash);
            const accessToken = params.get('access_token');
            
            // Send the token back to the server via a new request
            if (accessToken) {
                fetch('/token?access_token=' + accessToken)
                    .then(() => {
                        document.body.innerHTML += '<p>You can close this window now.</p>';
                    })
                    .catch(err => {
                        document.body.innerHTML += '<p>Error: ' + err.message + '</p>';
                    });
            } else {
                document.body.innerHTML += '<p>Error: No access token found</p>';
            }
        </script>
    </body>
    </html>";

            var buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/html";
            var responseOutput = response.OutputStream;
            responseOutput.Write(buffer, 0, buffer.Length);
            responseOutput.Close();
        }

        private void OnHttpRequestReceived(object sender, HttpListenerContext context)
        {
            Console.WriteLine($"Received request: {context.Request.Url}");

            if (context.Request.Url.AbsolutePath == "/token")
            {
                // Handle the token endpoint
                var accessToken = context.Request.QueryString["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    _authCompletionSource.TrySetResult(accessToken);
                }

                // Send a simple response
                SendSimpleResponse(context, "Token received");
                return;
            }

            // Handle the initial redirect from Google
            SendAuthCompletedResponse(context);
        }

        private void SendSimpleResponse(HttpListenerContext context, string message)
        {
            var response = context.Response;
            var buffer = System.Text.Encoding.UTF8.GetBytes(message);
            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/plain";
            var responseOutput = response.OutputStream;
            responseOutput.Write(buffer, 0, buffer.Length);
            responseOutput.Close();
        }
    }

}
