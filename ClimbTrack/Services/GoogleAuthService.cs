using Firebase.Auth;
using System.Web;

namespace ClimbTrack.Services
{

    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly string GoogleClientId;
        private readonly string GoogleRedirectUri;
        private readonly IFirebaseService _firebaseService;

        public GoogleAuthService(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;

            // Use your actual client ID from Google Cloud Console
            GoogleClientId = "905782286968-9t66s04engso3jvjpstj1slvaaecprdr.apps.googleusercontent.com";

            // Use the Firebase redirect URI for all platforms
            GoogleRedirectUri = "https://test1-7f758.firebaseapp.com/__/auth/handler";

            Console.WriteLine($"Initialized GoogleAuthService with redirect URI: {GoogleRedirectUri}");
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
            // Build the OAuth URL with code response type instead of token
            var authUrl = new Uri(
                "https://accounts.google.com/o/oauth2/auth" +
                $"?client_id={GoogleClientId}" +
                $"&redirect_uri={HttpUtility.UrlEncode(GoogleRedirectUri)}" +
                "&response_type=code" +  // Changed from token to code
                "&access_type=offline" +  // Request a refresh token
                "&prompt=consent" +  // Force consent screen
                "&scope=email%20profile");


            Console.WriteLine($"Auth URL: {authUrl}");

            // Open the URL in the browser
            await Browser.Default.OpenAsync(authUrl, BrowserLaunchMode.SystemPreferred);

            // Show detailed instructions for extracting the token
            await Application.Current.MainPage.DisplayAlert(
                "Extract Access Token",
                "After signing in, you'll be redirected to a page.\n\n" +
                "To get the access token:\n" +
                "1. Look at the URL in the browser\n" +
                "2. Find the part that starts with 'access_token='\n" +
                "3. Copy everything after 'access_token=' until the next '&' or the end of the URL\n" +
                "4. Return to this app and paste the token when prompted",
                "OK");

            // Since we can't capture the redirect in a mobile app when using the Firebase redirect URI,
            // we'll need to prompt the user to enter the token manually
            string accessToken = await Application.Current.MainPage.DisplayPromptAsync(
                "Google Sign-In",
                "Please copy the access token from the browser and paste it here:",
                maxLength: 2000);

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("No access token provided");
            }

            Console.WriteLine($"Received access token: {accessToken.Substring(0, Math.Min(10, accessToken.Length))}...");

            // Use the access token to sign in with Firebase
            return await _firebaseService.SignInWithGoogleAccessTokenAsync(accessToken);
        }

        private async Task<UserCredential> SignInWithGoogleOnDesktop()
        {
            // For desktop platforms, we can use a similar approach
            // but with different UI guidance
            var authUrl = new Uri(
                "https://accounts.google.com/o/oauth2/auth" +
                $"?client_id={GoogleClientId}" +
                $"&redirect_uri={HttpUtility.UrlEncode(GoogleRedirectUri)}" +
                "&response_type=token" +
                "&scope=email%20profile");

            // Open the URL in the browser
            await Browser.Default.OpenAsync(authUrl, BrowserLaunchMode.SystemPreferred);

            // Prompt for the token
            string accessToken = await Application.Current.MainPage.DisplayPromptAsync(
                "Google Sign-In",
                "Please sign in with Google in the browser, then copy the access token and paste it here:",
                maxLength: 2000);

            if (string.IsNullOrEmpty(accessToken))
            {
                throw new Exception("No access token provided");
            }

            // Use the access token to sign in with Firebase
            return await _firebaseService.SignInWithGoogleAccessTokenAsync(accessToken);
        }
    }

}
