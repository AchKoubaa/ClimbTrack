using ClimbTrack.Config;
using Firebase.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Maui.Authentication;

namespace ClimbTrack.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly string GoogleClientId;
        private readonly string GoogleRedirectUri;
        private readonly ILocalHttpServer _httpServer;
        private readonly IFirebaseService _firebaseService;
        private TaskCompletionSource<string> _authCompletionSource;

        public GoogleAuthService(ILocalHttpServer httpServer, IFirebaseService firebaseService)
        {
            _httpServer = httpServer;
            _firebaseService = firebaseService;

            // Use your actual client ID from Google Cloud Console
            GoogleClientId = "703559950083-kbfeqqi9evvmmgbn482ff65iaq5tchud.apps.googleusercontent.com";

            // Set the redirect URI based on platform
            if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
            {
                // For mobile, use a custom scheme URI
                GoogleRedirectUri = "com.yourcompany.climbtrack://oauth2redirect";
            }
            else
            {
                // For desktop, we'll use the local server URL
                GoogleRedirectUri = "http://localhost:3000/callback";
            }

            // For Android, use the Android client ID specifically
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Replace with your Android client ID (ends with .apps.googleusercontent.com)
                GoogleClientId = "703559950083-4ibgmelvb7nuebagh17dtovp827posti.apps.googleusercontent.com";
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
                if (DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS)
                {
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
            try
            {
                // Use Firebase OAuth endpoint with the correct parameters
                string firebaseAuthUrl = $"https://{FirebaseConfig.ProjectId}.firebaseapp.com/__/auth/handler?" +
                    $"apiKey={FirebaseConfig.ApiKey}" +
                    $"&providerId=google.com" +
                    $"&type=signInWithRedirect" +  // Add this parameter
                    $"&appName={FirebaseConfig.ProjectId}"; // Add this parameter

                Console.WriteLine($"Using Firebase auth URL: {firebaseAuthUrl}");

                // Get your application ID from the current app info
                string appPackageName = AppInfo.PackageName;
                string callbackUrl = "com.companyname.climbtrack://oauth2redirect";

                Console.WriteLine($"Using callback URL: {callbackUrl}");

                // Create a cancellation token source for timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new WebAuthenticatorOptions
                    {
                        Url = new Uri(firebaseAuthUrl),
                        CallbackUrl = new Uri(callbackUrl),
                        PrefersEphemeralWebBrowserSession = false
                    }).WaitAsync(cts.Token);

                // Try to extract the token using different possible property names
                string token = null;

                // Check for id_token
                if (result.Properties.TryGetValue("id_token", out var idToken) && !string.IsNullOrEmpty(idToken))
                {
                    token = idToken;
                    Console.WriteLine("Using ID token for authentication");
                }
                // Check for access_token
                else if (result.Properties.TryGetValue("access_token", out var accessToken) && !string.IsNullOrEmpty(accessToken))
                {
                    token = accessToken;
                    Console.WriteLine("Using access token for authentication");
                }
                // Check for firebase_token
                else if (result.Properties.TryGetValue("firebase_token", out var firebaseToken) && !string.IsNullOrEmpty(firebaseToken))
                {
                    token = firebaseToken;
                    Console.WriteLine("Using Firebase token for authentication");
                }
                // Check for token
                else if (result.Properties.TryGetValue("token", out var genericToken) && !string.IsNullOrEmpty(genericToken))
                {
                    token = genericToken;
                    Console.WriteLine("Using generic token for authentication");
                }
                else
                {
                    throw new Exception("No authentication token found in the result");
                }


                // Use the ID token with Firebase
                return await _firebaseService.SignInWithGoogleAccessTokenAsync(idToken);
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
            Console.WriteLine("Using desktop authentication flow with local server");

            // Generate PKCE values for enhanced security
            string codeVerifier = GenerateCodeVerifier();
            string codeChallenge = GenerateCodeChallenge(codeVerifier);
            string state = Guid.NewGuid().ToString("N");

            // Subscribe to the RequestReceived event
            _httpServer.RequestReceived += OnHttpRequestReceived;
            _authCompletionSource = new TaskCompletionSource<string>();

            try
            {
                // Start the HTTP server
                await _httpServer.StartAsync();
                string callbackUrl = $"{_httpServer.BaseUrl.TrimEnd('/')}/callback";

                Console.WriteLine($"Started local HTTP server on {_httpServer.BaseUrl}");

                // Build the authorization URL with PKCE
                var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={GoogleClientId}" +
                    $"&redirect_uri={Uri.EscapeDataString(callbackUrl)}" +
                    $"&response_type=code" + // Use code flow, not token flow
                    $"&code_challenge={codeChallenge}" +
                    $"&code_challenge_method=S256" +
                    $"&scope={Uri.EscapeDataString("openid profile email")}" +
                    $"&state={state}";

                // Open browser with authentication URL
                OpenBrowser(authUrl);
                Console.WriteLine($"Opened browser with auth URL");

                // Wait for the authentication to complete with a timeout
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
                var completedTask = await Task.WhenAny(_authCompletionSource.Task, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    throw new TimeoutException("Authentication timed out after 5 minutes");
                }

                var authCode = await _authCompletionSource.Task;

                if (!string.IsNullOrEmpty(authCode))
                {
                    Console.WriteLine("Authorization code obtained. Exchanging for tokens...");

                    // Exchange the authorization code for tokens
                    var tokenResult = await ExchangeCodeForTokensAsync(authCode, codeVerifier, callbackUrl);

                    if (tokenResult?.IdToken == null && tokenResult?.AccessToken == null)
                    {
                        throw new Exception("No tokens received from Google");
                    }

                    // Store refresh token if available
                    if (!string.IsNullOrEmpty(tokenResult.RefreshToken))
                    {
                        await SecureStorage.SetAsync("google_refresh_token", tokenResult.RefreshToken);
                    }

                    
                        // Fallback to access token
                        Console.WriteLine("Using access token for Firebase authentication");
                        return await _firebaseService.SignInWithGoogleAccessTokenAsync(tokenResult.AccessToken);
                    
                }
                else
                {
                    throw new Exception("No authorization code found in authentication result");
                }
            }
            finally
            {
                // Clean up
                _httpServer.RequestReceived -= OnHttpRequestReceived;
                _httpServer.Stop();
            }
        }

        private void OnHttpRequestReceived(object sender, HttpListenerContext context)
        {
            Console.WriteLine($"Received request: {context.Request.Url}");

            try
            {
                if (context.Request.Url.AbsolutePath == "/callback")
                {
                    // Extract the authorization code from the query parameters
                    var code = context.Request.QueryString["code"];
                    var error = context.Request.QueryString["error"];

                    if (!string.IsNullOrEmpty(code))
                    {
                        // Send a success response to the browser
                        SendSuccessResponse(context);

                        // Set the result to complete the task
                        _authCompletionSource.TrySetResult(code);
                    }
                    else
                    {
                        // Handle error
                        SendErrorResponse(context, error ?? "Unknown error");
                        _authCompletionSource.TrySetException(new Exception($"Authentication failed: {error}"));
                    }
                }
                else
                {
                    // Serve a simple page for any other request
                    SendSimpleResponse(context, "Authentication in progress...");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling request: {ex.Message}");
                try
                {
                    SendErrorResponse(context, "Internal server error");
                }
                catch { /* Ignore errors in error handling */ }
            }
        }

        private void OpenBrowser(string url)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch
            {
                // Fall back to platform-specific commands
                if (OperatingSystem.IsWindows())
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (OperatingSystem.IsLinux())
                {
                    Process.Start("xdg-open", url);
                }
                else if (OperatingSystem.IsMacOS())
                {
                    Process.Start("open", url);
                }
                else
                {
                    Console.WriteLine($"Please open this URL manually: {url}");
                }
            }
        }

        private async Task<TokenResponse> ExchangeCodeForTokensAsync(string authCode, string codeVerifier, string redirectUri)
        {
            using (var httpClient = new HttpClient())
            {
                // Set a reasonable timeout
                httpClient.Timeout = TimeSpan.FromSeconds(30);

                // Prepare the token request
                var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = authCode,
                    ["client_id"] = GoogleClientId,
                    ["code_verifier"] = codeVerifier,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                });

                // Make the token request
                var response = await httpClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
                var responseContent = await response.Content.ReadAsStringAsync();

                // Check for errors
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Token exchange failed: {response.StatusCode}, {responseContent}");
                }

                // Parse the response
                return System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(responseContent);
            }
        }

        /// <summary>
        /// Generates a random code verifier for PKCE.
        /// </summary>
        private string GenerateCodeVerifier()
        {
            byte[] buffer = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            return Convert.ToBase64String(buffer)
                .Replace('+', '-')
                .Replace('/', '_')
                .Replace("=", "");
        }

        /// <summary>
        /// Generates a code challenge from the code verifier using SHA-256.
        /// </summary>
        private string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));

                return Convert.ToBase64String(challengeBytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace("=", "");
            }
        }

        private void SendSuccessResponse(HttpListenerContext context)
        {
            string responseHtml = @"
<html>
<head>
    <title>Authentication Successful</title>
    <style>
        body { font-family: Arial, sans-serif; text-align: center; padding-top: 50px; background-color: #f5f5f5; }
        .container { background-color: white; max-width: 500px; margin: 0 auto; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        h1 { color: #4285F4; }
        p { margin: 20px 0; color: #555; }
    </style>
</head>
<body>
    <div class='container'>
        <h1>Authentication Successful</h1>
        <p>You have successfully signed in with Google.</p>
        <p>You can close this window and return to ClimbTrack.</p>
    </div>
</body>
</html>";

            SendHtmlResponse(context, responseHtml);
        }


        private void SendErrorResponse(HttpListenerContext context, string errorMessage)
        {
            string responseHtml = $@"
<html>
<head>
    <title>Authentication Error</title>
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; padding-top: 50px; background-color: #f5f5f5; }}
        .container {{ background-color: white; max-width: 500px; margin: 0 auto; padding: 30px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        h1 {{ color: #EA4335; }}
        p {{ margin: 20px 0; color: #555; }}
        .error {{ color: #EA4335; }}
    </style>
</head>
<body>
    <div class='container'>
        <h1>Authentication Error</h1>
        <p>There was a problem signing in with Google.</p>
        <p class='error'>{HttpUtility.HtmlEncode(errorMessage)}</p>
        <p>Please close this window and try again.</p>
    </div>
</body>
</html>";

            SendHtmlResponse(context, responseHtml);
        }

        private void SendSimpleResponse(HttpListenerContext context, string message)
        {
            string responseHtml = $@"
<html>
<head>
    <title>ClimbTrack Authentication</title>
    <style>
        body {{ font-family: Arial, sans-serif; text-align: center; padding-top: 50px; }}
        p {{ margin: 20px 0; }}
    </style>
</head>
<body>
    <p>{HttpUtility.HtmlEncode(message)}</p>
</body>
</html>";

            SendHtmlResponse(context, responseHtml);
        }

        private void SendHtmlResponse(HttpListenerContext context, string html)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            context.Response.ContentLength64 = buffer.Length;
            context.Response.ContentType = "text/html";
            context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        /// <summary>
        /// Class representing the response from the OAuth token endpoint.
        /// </summary>
        private class TokenResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("access_token")]
            public string AccessToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("id_token")]
            public string IdToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("refresh_token")]
            public string RefreshToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
            public int ExpiresIn { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("token_type")]
            public string TokenType { get; set; }
        }
    }

}
