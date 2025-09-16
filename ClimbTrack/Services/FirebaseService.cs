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

        public async Task<UserCredential> SignInWithEmailAndPassword(string email, string password)
        {
            try
            {
                var userCredential = await _authClient.SignInWithEmailAndPasswordAsync(email, password);
                // Verify that we received valid credentials
                if (userCredential == null)
                {
                    throw new InvalidOperationException("Failed to sign in: No user credentials returned.");
                }
                await _authService.SaveAuthData(userCredential);
                return userCredential;
            }
            catch (Firebase.Auth.FirebaseAuthHttpException ex)
            {
                // Handle specific HTTP errors from Firebase Auth
                string errorMessage = ParseFirebaseAuthError(ex);

                // Log the error with your error handling service
                await _errorHandlingService.LogErrorAsync(errorMessage, "SignInWithEmailAndPassword", false);

                // Throw a more user-friendly exception
                throw new AuthenticationException(errorMessage, ex);
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                // Handle other Firebase Auth errors
                await _errorHandlingService.HandleAuthenticationExceptionAsync(ex, "SignInWithEmailAndPassword");
                throw;
            }
            catch (Exception ex)
            {
                // Handle unexpected errors
                await _errorHandlingService.HandleExceptionAsync(ex, "SignInWithEmailAndPassword", true);
                throw;
            }
        }

        private string ParseFirebaseAuthError(Firebase.Auth.FirebaseAuthHttpException ex)
        {
            // Extract the error message from the response
            if (ex.ResponseData != null && ex.ResponseData.Contains("INVALID_LOGIN_CREDENTIALS"))
            {
                return "Invalid email or password. Please try again.";
            }
            else if (ex.ResponseData != null && ex.ResponseData.Contains("EMAIL_NOT_FOUND"))
            {
                return "Email not found. Please check your email or register.";
            }
            else if (ex.ResponseData != null && ex.ResponseData.Contains("INVALID_PASSWORD"))
            {
                return "Incorrect password. Please try again.";
            }
            else if (ex.ResponseData != null && ex.ResponseData.Contains("USER_DISABLED"))
            {
                return "This account has been disabled. Please contact support.";
            }
            else if (ex.ResponseData != null && ex.ResponseData.Contains("TOO_MANY_ATTEMPTS_TRY_LATER"))
            {
                return "Too many failed login attempts. Please try again later or reset your password.";
            }

            // Default message for other errors
            return "Authentication failed. Please try again.";
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