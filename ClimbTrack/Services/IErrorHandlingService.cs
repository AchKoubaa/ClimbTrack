using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface IErrorHandlingService
    {
        Task HandleAuthenticationExceptionAsync(Exception ex, string context = null);
        Task HandleExceptionAsync(Exception ex, string context = null, bool showToUser = true);
        Task LogErrorAsync(string message, string context = null, bool showToUser = false);
        Task<bool> HandleHttpErrorAsync(HttpResponseMessage response, string context = null);
        Task ShowErrorToUserAsync(string message, string title = "Error");
        string GetUserFriendlyMessage(Exception ex);

    }
}
