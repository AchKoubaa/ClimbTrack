using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface IServiceAccountService
    {
        Task<string> CreateCustomTokenAsync(string uid, Dictionary<string, object> claims = null);
        Task<Dictionary<string, object>> GetUserWithServiceAccountAsync(string uid);
        Task<Dictionary<string, object>> CreateUserWithServiceAccountAsync(string email, string password, string displayName = null);
        Task<Dictionary<string, object>> VerifyIdTokenAsync(string idToken);
    }
}
