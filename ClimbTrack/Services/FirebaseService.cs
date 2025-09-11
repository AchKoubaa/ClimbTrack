using ClimbTrack.Config;
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

        public FirebaseService(
            IAuthService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            

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
                Console.WriteLine($"Firebase signup error: {ex.Message}");
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
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                Console.WriteLine($"Firebase signin error: {ex.Message}");
                throw;
            }
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
            catch (FirebaseAuthException)
            {
                return false;
            }
        }

        public async Task<FirebaseClient> GetAuthenticatedClientAsync()
        {
            var user = GetCurrentUser();
            if (user == null)
            {
                Debug.WriteLine("No authenticated user for database access");
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
                Debug.WriteLine($"Error creating authenticated database client: {ex.Message}");
                return null;
            }
        }
        #endregion


    }
}