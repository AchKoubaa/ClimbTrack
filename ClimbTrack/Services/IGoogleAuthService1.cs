using Firebase.Auth;
namespace ClimbTrack.Services
{
    public interface IGoogleAuthService
    {
        Task<UserCredential> SignInWithGoogleAsync();
    }
}