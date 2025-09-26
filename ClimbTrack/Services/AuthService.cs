using ClimbTrack.Config;
using Firebase.Auth;
using Firebase.Database;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class AuthService : IAuthService, IDisposable
    {
        private const string TokenKey = "firebase_token";
        private const string UserIdKey = "firebase_user_id";
        private const string UserEmailKey = "firebase_user_email";
        private const string RefreshTokenKey = "firebase_refresh_token";

        private readonly FirebaseAuthClient _authClient;

        // Add the event
        public event EventHandler AuthStateChanged;
        public AuthService()
        {
            // Configure Firebase Auth using the FirebaseConfig class
            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = FirebaseConfig.AuthDomain,
                Providers = new Firebase.Auth.Providers.FirebaseAuthProvider[]
                {
                    new Firebase.Auth.Providers.EmailProvider(),
                    new Firebase.Auth.Providers.GoogleProvider(),
                    //new Firebase.Auth.Providers.AnonymousProvider()
                }
            };

            _authClient = new FirebaseAuthClient(config);

            // Subscribe to Firebase auth state changes
            _authClient.AuthStateChanged += OnFirebaseAuthStateChanged;
        }

       
        public async Task SaveAuthData(UserCredential userCredential)
        {
            if (userCredential == null || userCredential.User == null)
                throw new ArgumentNullException(nameof(userCredential));

            // Get the token from the user
            string token = await userCredential.User.GetIdTokenAsync();
           // string refreshToken = userCredential.User.RefreshToken;

            await SecureStorage.Default.SetAsync(TokenKey, token);
           // await SecureStorage.Default.SetAsync(RefreshTokenKey, refreshToken);
            await SecureStorage.Default.SetAsync(UserIdKey, userCredential.User.Uid);
            await SecureStorage.Default.SetAsync(UserEmailKey, userCredential.User.Info.Email);
        }

        public async Task<bool> IsTokenValid()
        {
            try
            {
                string token = await GetToken();
                if (string.IsNullOrEmpty(token))
                    return false;

                // Decode token to check expiration
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // Check if token is expired
                var expiration = jwtToken.ValidTo;
                if (expiration < DateTime.UtcNow)
                {
                    Debug.WriteLine("Token is expired");
                    return false;
                }

                // Additional validation - check if timestamps make sense
                if (jwtToken.ValidFrom > DateTime.UtcNow)
                {
                    Debug.WriteLine("Token has invalid issue date (in the future)");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error validating token: {ex.Message}");
                return false;
            }
        }

        public async Task<string> GetToken()
        {
            try
            {
                var token = await SecureStorage.Default.GetAsync(TokenKey);

                // If token exists but might be expired, try to refresh it
                if (!string.IsNullOrEmpty(token) && _authClient.User != null)
                {
                    try
                    {
                        // Force refresh the token
                        // Check if token is about to expire (within 5 minutes)
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        var jwtToken = handler.ReadJwtToken(token);
                        var expiration = jwtToken.ValidTo;

                        if (expiration < DateTime.UtcNow.AddMinutes(5))
                        {
                            Debug.WriteLine("Token is about to expire, refreshing...");
                            // Force refresh the token
                            token = await _authClient.User.GetIdTokenAsync(true);
                            await SecureStorage.Default.SetAsync(TokenKey, token);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error refreshing token: {ex.Message}");
                        // Continue with the existing token
                    }
                }

                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving token: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetUserId()
        {
            try
            {
                return await SecureStorage.Default.GetAsync(UserIdKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user ID: {ex.Message}");
                return null;
            }
        }

        public async Task<string> GetUserEmail()
        {
            try
            {
                return await SecureStorage.Default.GetAsync(UserEmailKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving user email: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> IsAuthenticated()
        {
            try
            {
                var token = await GetToken();
                return !string.IsNullOrEmpty(token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking authentication: {ex.Message}");
                return false;
            }
        }

        public async void Logout()
        {
            try
            {
                //// Sign out from Firebase
                if (_authClient.User != null)
                {
                    _authClient.SignOut();
                }

                // Clear stored authentication data
                SecureStorage.Default.Remove(TokenKey);
                SecureStorage.Default.Remove(RefreshTokenKey);
                SecureStorage.Default.Remove(UserIdKey);
                SecureStorage.Default.Remove(UserEmailKey);

               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during logout: {ex.Message}");
            }
            return;
        }
        public User GetCurrentUser()
        {
            return _authClient.User;
        }

        // Add method to handle authentication failures
        public async Task HandleAuthenticationFailure()
        {
            Logout();

            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("///login");

                await Shell.Current.DisplayAlert(
                        "Session Expired",
                        "Your session has expired. Please log in again.",
                        "OK");
            }
        }

        public async Task<FirebaseClient> GetAuthenticatedClientAsync()
        {
            // First check if token is valid
            if (!await IsTokenValid())
            {
                Debug.WriteLine("Invalid or expired token detected");
                await HandleAuthenticationFailure();
                return null;
            }

            try
            {
               
                // Create and return an authenticated client with auto-refresh capability
                var firebaseClient = new FirebaseClient(
                    FirebaseConfig.DatabaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = async () => {
                            // This will be called whenever the client needs a token
                            if (!await IsTokenValid())
                            {
                                Debug.WriteLine("Token invalid during request");
                                await HandleAuthenticationFailure();
                                return null;
                            }
                            return await GetToken();
                        }
                    });
                return firebaseClient;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating authenticated database client: {ex.Message}");
                return null;
            }
        }

        private void OnFirebaseAuthStateChanged(object sender, UserEventArgs e)
        {
            // Notify subscribers that auth state has changed
            AuthStateChanged?.Invoke(this, EventArgs.Empty);
        }
        public void Dispose()
        {
            // Unsubscribe from Firebase auth state changes
            if (_authClient != null)
            {
                _authClient.AuthStateChanged -= OnFirebaseAuthStateChanged;
            }
        }
    }
}