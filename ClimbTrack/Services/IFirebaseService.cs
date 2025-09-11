using ClimbTrack.Models;
using Firebase.Auth;
using Firebase.Database;
using System.Collections.ObjectModel;

namespace ClimbTrack.Services
{
    public interface IFirebaseService
    {
        // Auth Methods
        User GetCurrentUser();
        Task<UserCredential> SignUpWithEmailAndPassword(string email, string password);
        Task<UserCredential> SignInWithEmailAndPassword(string email, string password);
        Task ResetPassword(string email);
        //Task<string> RefreshTokenAsync(string refreshToken);
        Task<UserCredential> SignInWithGoogleAccessTokenAsync(string accessToken);
        Task<UserCredential> SignInAnonymouslyAsync();
        //Task<string> SendSignInLinkToEmailAsync(string email);
        Task<string> SendVerificationCodeEmailAsync(string email);
        Task<UserCredential> VerifyCodeAndSignInAsync(string email, string code);
        Task<bool> ConvertAnonymousUserToEmailUser(string email, string password);
        Task<bool> EnsureAuthenticatedAsync();
        Task<FirebaseClient> GetAuthenticatedClientAsync();




    }
}
