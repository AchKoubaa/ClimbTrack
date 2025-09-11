using Firebase.Auth;
using Firebase.Database;

namespace ClimbTrack.Services
{
    public interface IAuthService
    {
        Task SaveAuthData(UserCredential userCredential);
        Task<string> GetToken();
        Task<string> GetUserId();
        Task<string> GetUserEmail();
        Task<bool> IsAuthenticated();
        Task Logout();
        User GetCurrentUser();
        Task<FirebaseClient> GetAuthenticatedClientAsync();

        Task<bool> IsTokenValid();
        Task HandleAuthenticationFailure();
        // Add this event
        event EventHandler AuthStateChanged;


    }
}
