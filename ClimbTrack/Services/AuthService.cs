using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class AuthService : IAuthService
    {
        private const string TokenKey = "firebase_token";
        private const string UserIdKey = "firebase_user_id";
        private const string UserEmailKey = "firebase_user_email";

        private readonly IFirebaseService _firebaseService;

        public AuthService(IFirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        public async Task SaveAuthData(UserCredential userCredential)
        {
            // Get the token from the user
            string token = await userCredential.User.GetIdTokenAsync();

            await SecureStorage.Default.SetAsync(TokenKey, token);
            await SecureStorage.Default.SetAsync(UserIdKey, userCredential.User.Uid);
            await SecureStorage.Default.SetAsync(UserEmailKey, userCredential.User.Info.Email);
        }

        public async Task<string> GetToken()
        {
            return await SecureStorage.Default.GetAsync(TokenKey);
        }

        public async Task<string> GetUserId()
        {
            return await SecureStorage.Default.GetAsync(UserIdKey);
        }

        public async Task<string> GetUserEmail()
        {
            return await SecureStorage.Default.GetAsync(UserEmailKey);
        }

        public async Task<bool> IsAuthenticated()
        {
            var token = await GetToken();
            return !string.IsNullOrEmpty(token);
        }

        public async Task Logout()
        {
            SecureStorage.Default.Remove(TokenKey);
            SecureStorage.Default.Remove(UserIdKey);
            SecureStorage.Default.Remove(UserEmailKey);
        }
    }
}
