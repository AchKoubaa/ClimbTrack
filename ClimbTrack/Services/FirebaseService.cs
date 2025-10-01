using ClimbTrack.Config;
using ClimbTrack.Exceptions;
using ClimbTrack.Models;
using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Database;
using FirebaseAdmin.Auth;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FirebaseAuthException = Firebase.Auth.FirebaseAuthException;

namespace ClimbTrack.Services
{
    public class FirebaseService : IFirebaseService
    {
        private readonly IAuthService _authService;
        private readonly FirebaseAuthClient _authClient;
        private readonly HttpClient _httpClient;
        private readonly IErrorHandlingService _errorHandlingService;

        public FirebaseService(
            IAuthService authService,
    IErrorHandlingService errorHandlingService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));


            // Configure Firebase Auth
            var config = new FirebaseAuthConfig
            {
                ApiKey = FirebaseConfig.ApiKey,
                AuthDomain = FirebaseConfig.AuthDomain,
                Providers = new Firebase.Auth.Providers.FirebaseAuthProvider[]
                {
                    new EmailProvider(),
                    new GoogleProvider(),
                    //new AnonymousProvider()
                }
            };

            _authClient = new FirebaseAuthClient(config);
            _httpClient = new HttpClient();
        }

        #region Auth Methods

        public User GetCurrentUser()
        {
           
            return _authClient.User;
        }

        public async Task<UserCredential> SignUpWithEmailAndPassword(string email, string password)
        {
            try
            {
                var userCredential = await _authClient.CreateUserWithEmailAndPasswordAsync(email, password);
                await _authService.SaveAuthData(userCredential);
                return userCredential;
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "SignUpWithEmailAndPassword", true);
                
                throw;
            }
        }

        public async Task<GeneralResponse<UserCredential>> SignInWithEmailAndPassword(string email, string password)
        {
            try
            {
                var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);
                // Verify that we received valid credentials
                if (userCredential == null)
                {
                    return GeneralResponse<UserCredential>.Failure(
                        "Failed to sign in: No user credentials returned.",
                        null);
                }
                await _authService.SaveAuthData(userCredential);
                return GeneralResponse<UserCredential>.Success(userCredential, "Sign in successful"); 
            }
            catch (Firebase.Auth.FirebaseAuthHttpException ex)
            {
                // Handle specific HTTP errors from Firebase Auth
                string errorMessage = ParseFirebaseAuthError(ex);
                string actionNeeded = GetActionForAuthError(ex);

                // Log the error with your error handling service
                await _errorHandlingService.LogErrorAsync(errorMessage, "SignInWithEmailAndPassword", false);

                // Return a failure response with user-friendly message and action needed
                return GeneralResponse<UserCredential>.Failure($"{errorMessage}. {actionNeeded}");
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                // Handle other Firebase Auth errors
                await _errorHandlingService.HandleAuthenticationExceptionAsync(ex, "SignInWithEmailAndPassword");

                string errorMessage = GetUserFriendlyMessage(ex);
                string actionNeeded = GetActionForAuthError(ex);

                return GeneralResponse<UserCredential>.Failure($"{errorMessage}. {actionNeeded}");
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                await _errorHandlingService.HandleExceptionAsync(ex, "SignInWithEmailAndPassword", true);
                return GeneralResponse<UserCredential>.Failure(
             "An unexpected error occurred. Please try again later or contact support.");
            }
        }

        // Helper method to parse Firebase auth errors
        private string ParseFirebaseAuthError(Firebase.Auth.FirebaseAuthHttpException ex)
        {
            // Extract the error code from the exception
            string errorCode = ex.Reason!.ToString() ?? "Unknown";

            switch (errorCode)
            {
                case "EmailNotFound":
                    return "The email address you entered doesn't exist in our records";
                case "WrongPassword":
                    return "The password you entered is incorrect";
                case "UserDisabled":
                    return "This account has been disabled";
                case "TooManyAttempts":
                    return "Too many failed login attempts. Please try again later";
                case "InvalidEmail":
                    return "The email address format is invalid";
                case "NetworkRequestFailed":
                    return "Network connection issue. Please check your internet connection";
                default:
                    return $"Authentication error: {errorCode}";
            }
        }

        // Helper method to suggest actions based on error
        private string GetActionForAuthError(Exception ex)
        {
            string errorCode = "";

            if (ex is Firebase.Auth.FirebaseAuthHttpException httpEx)
            {
                errorCode = httpEx.Reason!.ToString() ?? "";
            }
            else if (ex is Firebase.Auth.FirebaseAuthException authEx)
            {
                errorCode = authEx.Reason!.ToString() ?? "";
            }

            switch (errorCode)
            {
                case "EmailNotFound":
                    return "Please check your email address or sign up for a new account";
                case "WrongPassword":
                    return "Please try again or use the 'Forgot Password' option";
                case "UserDisabled":
                    return "Please contact support for assistance";
                case "TooManyAttempts":
                    return "Try again later or reset your password";
                case "NetworkRequestFailed":
                    return "Check your internet connection and try again";
                case "InvalidEmail":
                    return "Please enter a valid email address";
                default:
                    return "If the problem persists, please contact support";
            }
        }

        // Helper method for other Firebase auth exceptions
        private string GetUserFriendlyMessage(Firebase.Auth.FirebaseAuthException ex)
        {
            // You can expand this based on the specific error types you encounter
            return ex.Message.Contains("password")
                ? "There was a problem with your password"
                : "There was a problem signing you in";
        }

        public async Task ResetPassword(string email)
        {
            try
            {
                await _authClient.ResetEmailPasswordAsync(email);
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                Console.WriteLine($"Firebase reset password error: {ex.Message}");
                throw;
            }
        }

        //public async Task<string> RefreshTokenAsync(string refreshToken)
        //{
        //    try
        //    {
        //        var result = await _authClient.RefreshAuthAsync(new RefreshAuth { RefreshToken = refreshToken });
        //        return result.RefreshToken;
        //    }
        //    catch (Firebase.Auth.FirebaseAuthException ex)
        //    {
        //        Console.WriteLine($"Firebase token refresh error: {ex.Message}");
        //        throw;
        //    }
        //}

        public async Task<UserCredential> SignInWithGoogleAccessTokenAsync(string accessToken)
        {
            try
            {
                var credential = GoogleProvider.GetCredential(accessToken);
                var userCredential = await _authClient.SignInWithCredentialAsync(credential);
                await _authService.SaveAuthData(userCredential);
                return userCredential;
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                Console.WriteLine($"Firebase Google signin error: {ex.Message}");
                throw;
            }
        }

        public async Task<UserCredential> SignInAnonymouslyAsync()
        {
            try
            {
                var userCredential = await _authClient.SignInAnonymouslyAsync();
                await _authService.SaveAuthData(userCredential);
                return userCredential;
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                Console.WriteLine($"Firebase anonymous signin error: {ex.Message}");
                throw;
            }
        }

        //public async Task<string> SendSignInLinkToEmailAsync(string email)
        //{
        //    try
        //    {
        //        var actionCodeSettings = new ActionCodeSettings
        //        {
        //            Url = $"https://{FirebaseConfig.AuthDomain}/finishSignUp?email=" + email,
        //            HandleCodeInApp = true
        //        };

        //        await _authClient.SendSignInLinkToEmailAsync(email, actionCodeSettings);
        //        return "Email link sent successfully";
        //    }
        //    catch (FirebaseAuthException ex)
        //    {
        //        Console.WriteLine($"Firebase email link error: {ex.Message}");
        //        throw;
        //    }
        //}

        public async Task<string> SendVerificationCodeEmailAsync(string email)
        {
            try
            {
                // For email verification, Firebase typically sends a link rather than a code
                if (_authClient.User != null)
                {
                   // await _authClient.User.SendEmailVerificationAsync();
                    return "Verification email sent";
                }
                else
                {
                    throw new InvalidOperationException("No user is currently signed in");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase verification code error: {ex.Message}");
                throw;
            }
        }

        public async Task<UserCredential> VerifyCodeAndSignInAsync(string email, string code)
        {
            try
            {
                // This functionality might require custom implementation
                // as Firebase typically uses links rather than codes for email verification

                // One approach could be to use Firebase Phone Auth or implement a custom verification flow
                throw new NotImplementedException("Email code verification requires custom implementation");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Firebase code verification error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ConvertAnonymousUserToEmailUser(string email, string password)
        {
            try
            {
                if (_authClient.User == null || !_authClient.User.IsAnonymous)
                {
                    return false;
                }

                // Create email credential
                var credential = EmailProvider.GetCredential(email, password);

                // Link the anonymous user with the email credential
                await _authClient.User.LinkWithCredentialAsync(credential);
                return true;
            }
            catch (FirebaseAuthException ex)
            {
                Console.WriteLine($"Firebase account conversion error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> EnsureAuthenticatedAsync()
        {
            try
            {
                if (_authClient.User == null)
                {
                    return false;
                }

                // Check if token is valid and not expired
                var token = await _authClient.User.GetIdTokenAsync(true);
                return !string.IsNullOrEmpty(token);
            }
            catch (FirebaseAuthException ex)
            {
                await _errorHandlingService.HandleAuthenticationExceptionAsync(ex, "EnsureAuthenticatedAsync");
                return false;
            }
        }

        public async Task<FirebaseClient> GetAuthenticatedClientAsync()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                await _errorHandlingService.LogErrorAsync("No authenticated user for database access", "GetAuthenticatedClientAsync");
                
                return null;
            }

            try
            {
                // Get the authentication token
                var token = await user.GetIdTokenAsync();

                // Create and return an authenticated client
                return new FirebaseClient(
                    FirebaseConfig.DatabaseUrl,
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(token)
                    });
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "GetAuthenticatedClientAsync", false);
                
                return null;
            }
        }
        #endregion


    }
}