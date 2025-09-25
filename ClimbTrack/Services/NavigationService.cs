using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class NavigationService : INavigationService
    {
        private bool _isNavigating = false;
        private readonly IAuthService _authService;

        public NavigationService(IAuthService authService)
        {
            _authService = authService;
        }

        public async Task NavigateToAsync(string route, IDictionary<string, object> parameters = null)
        {
            if (_isNavigating || Shell.Current == null)
                return;

            try
            {
                _isNavigating = true;

                // Check authentication for protected routes
                if (!await CheckAuthenticationForRoute(route))
                    return;

                // Normalize route to prevent navigation issues
                string normalizedRoute = NormalizeRoute(route);

                // Navigate with or without parameters
                if (parameters != null)
                    await Shell.Current.GoToAsync(normalizedRoute, parameters);
                else
                    await Shell.Current.GoToAsync(normalizedRoute);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        public async Task NavigateToMainPage()
        {           
            await Shell.Current.GoToAsync("home");
        }

        public async Task NavigateToLoginPage()
        {
            
                await Shell.Current.GoToAsync("login");
            
         }

        public async Task GoBackAsync()
        {
            if (Shell.Current?.Navigation.NavigationStack.Count > 1)
                await Shell.Current.GoToAsync("..");
        }

        public async Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
        {
            if (Shell.Current != null)
                return await Shell.Current.DisplayPromptAsync(title, message, accept, cancel);

          

            return null;
        }

        public async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            if (Shell.Current != null)
                await Shell.Current.DisplayAlert(title, message, cancel);
            
        }
       
        public async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            if (Shell.Current != null)
                return await Shell.Current.DisplayAlert(title, message, accept, cancel);
            

            return false;
        }

        // Helper methods
        private string NormalizeRoute(string route)
        {
            // Fix tab navigation paths to prevent issues like //main/profile/home
            if (route.Contains("/main/") && route.Count(c => c == '/') > 2)
            {
                var segments = route.Split('/');
                if (segments.Length >= 3)
                    return $"//main/{segments[segments.Length - 1]}";
            }
            return route;
        }

        private async Task<bool> CheckAuthenticationForRoute(string route)
        {
            // Public routes don't require authentication
            var publicRoutes = new[] { "//login", "//register", "login", "register" };
            if (publicRoutes.Any(r => route.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
                return true;

            // Check if user is authenticated
            if (!await _authService.IsAuthenticated())
            {
                await Shell.Current.GoToAsync("login");
                return false;
            }

            return true;
        }
    }
}