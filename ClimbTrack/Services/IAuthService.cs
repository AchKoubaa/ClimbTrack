using Firebase.Auth;

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
    }
}
