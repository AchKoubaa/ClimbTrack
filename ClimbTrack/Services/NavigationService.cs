using ClimbTrack.ViewModels;
using ClimbTrack.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
            try
            {
                if (_isNavigating)
                {
                    Debug.WriteLine($"Navigation already in progress, ignoring duplicate request to {route}");
                    return;

                    
                }

                _isNavigating = true;

                // Verifica l'autenticazione per le route protette
                if (!await CheckAuthenticationForRoute(route))
                {
                    _isNavigating = false;
                    return;
                }
                

                if (Shell.Current == null)
                {
                    Debug.WriteLine($"NavigateToAsync: Shell.Current è null. Impossibile navigare a {route}");
                    _isNavigating = false;
                    return;
                }

                Debug.WriteLine($"Navigating to {route}");

                    if (parameters != null)
                    {
                        await Shell.Current.GoToAsync(route, parameters);
                    }
                    else
                    {
                        await Shell.Current.GoToAsync(route);
                    }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante la navigazione a {route}: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private async Task<bool> CheckAuthenticationForRoute(string route)
        {
            // Pagine che non richiedono autenticazione
            var publicRoutes = new[] { "//login", "//register", "login", "register" };

            // Se la route è pubblica, permetti la navigazione
            if (publicRoutes.Any(r => route.StartsWith(r, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Verifica se l'utente è autenticato
            bool isAuthenticated = await _authService.IsAuthenticated();
            if (!isAuthenticated)
            {
                Debug.WriteLine($"User not authenticated, redirecting to login page instead of {route}");

                // Reindirizza alla pagina di login
                await NavigateToLoginPage();
                return false;
            }

            // L'utente è autenticato, permetti la navigazione
            return true;
        }

        public async Task NavigateToMainPage()
        {
            try
            {
                // Verifica se l'utente è autenticato
                bool isAuthenticated = await _authService.IsAuthenticated();
                if (!isAuthenticated)
                {
                    Debug.WriteLine("User not authenticated, redirecting to login page");
                    await NavigateToLoginPage();
                    return;
                }

                if (Shell.Current == null)
                {
                    Debug.WriteLine("NavigateToMainPage: Shell.Current è null");

                    // Imposta direttamente la MainPage
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Application.Current.MainPage = new AppShell();
                    });

                    // Attendi un momento per assicurarti che Shell sia inizializzata
                    await Task.Delay(100);

                    // Verifica nuovamente se Shell.Current è disponibile
                    if (Shell.Current == null)
                    {
                        Debug.WriteLine("Shell.Current è ancora null dopo l'inizializzazione");
                        return;
                    }
                }

                // Naviga alla HomePage
                await Shell.Current.GoToAsync("//home");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante la navigazione alla MainPage: {ex.Message}");
            }
        }

        public async Task NavigateToLoginPage()
        {
            if (_isNavigating)
            {
                Debug.WriteLine("Navigation already in progress, ignoring duplicate request to login page");
                return;
            }

            try
            {
                _isNavigating = true;

                // Check if we're already on the login page
                var currentPage = GetCurrentPage();
                if (currentPage is LoginPage || currentPage?.GetType().Name.Contains("Login") == true)
                {
                    Debug.WriteLine("Already on LoginPage, avoiding navigation");
                    return;
                }

                Debug.WriteLine("Navigating to login page");
                await Shell.Current.GoToAsync("//login");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private Page GetCurrentPage()
        {
            if (Application.Current?.MainPage is Shell shell)
            {
                return shell.CurrentPage;
            }
            else if (Application.Current?.MainPage is NavigationPage navPage)
            {
                return navPage.CurrentPage;
            }

            return Application.Current?.MainPage;
        }

        public async Task GoBackAsync()
        {
            try
            {
                if (Shell.Current == null)
                {
                    Debug.WriteLine("GoBackAsync: Shell.Current è null");
                    return;
                }

                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante la navigazione indietro: {ex.Message}");
            }
        }

        public async Task<string> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel")
        {
            try
            {
                if (Shell.Current == null)
                {
                    Debug.WriteLine("DisplayPromptAsync: Shell.Current è null");

                    if (Application.Current?.MainPage != null)
                    {
                        return await Application.Current.MainPage.DisplayPromptAsync(title, message, accept, cancel);
                    }

                    return null;
                }

                return await Shell.Current.DisplayPromptAsync(title, message, accept, cancel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante la visualizzazione del prompt: {ex.Message}");
                return null;
            }
        }

        public async Task DisplayAlertAsync(string title, string message, string cancel)
        {
            try
            {
                if (Shell.Current == null)
                {
                    Debug.WriteLine("DisplayAlertAsync: Shell.Current è null");

                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert(title, message, cancel);
                    }

                    return;
                }

                await Shell.Current.DisplayAlert(title, message, cancel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante la visualizzazione dell'alert: {ex.Message}");
            }
        }

        public async Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel)
        {
            try
            {
                if (Shell.Current == null)
                {
                    Debug.WriteLine("DisplayAlertAsync: Shell.Current è null");

                    if (Application.Current?.MainPage != null)
                    {
                        return await Application.Current.MainPage.DisplayAlert(title, message, accept, cancel);
                    }

                    return false;
                }

                return await Shell.Current.DisplayAlert(title, message, accept, cancel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante la visualizzazione dell'alert con conferma: {ex.Message}");
                return false;
            }
        }

       
    }
}