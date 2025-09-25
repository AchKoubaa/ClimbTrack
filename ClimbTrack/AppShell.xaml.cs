using ClimbTrack.Services;
using ClimbTrack.ViewModels;
using ClimbTrack.Views;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Google.Api;

namespace ClimbTrack
{
    public partial class AppShell : Shell, IDisposable
    {
        private bool _disposed = false;
        private bool _isNavigating = false;
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;

        public AppShell(IAuthService authService, IDatabaseService databaseService)
        {
            InitializeComponent();
            _authService = authService;
            _databaseService = databaseService;

            // Register routes for navigation
            RegisterRoutes();

            // Subscribe to navigation events to update UI based on route
            Navigating += OnNavigating;

            // Initialize app and check authentication
            _ = InitializeAppAndCheckAuth();
          
        }

        private void RegisterRoutes()
        {
            try
            {
                Routing.RegisterRoute("login", typeof(LoginPage));
                Routing.RegisterRoute("register", typeof(RegisterPage));
                Routing.RegisterRoute("home", typeof(HomePage));
                Routing.RegisterRoute("dashboard", typeof(DashboardPage));
                Routing.RegisterRoute("training", typeof(TrainingPage));
                Routing.RegisterRoute("profile", typeof(ProfilePage));
                Routing.RegisterRoute("editProfile", typeof(EditProfilePage));
                Routing.RegisterRoute("sessionDetails", typeof(SessionDetailsPage));
                Routing.RegisterRoute("admin", typeof(AdminPage));
                Routing.RegisterRoute("historical", typeof(HistoricalPage));

                Debug.WriteLine("Routes registered successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering routes: {ex.Message}");
            }
        }

        private void OnNavigating(object sender, ShellNavigatingEventArgs e)
        {
            // Prevent navigation loops
            if (_isNavigating) return;
            _isNavigating = true;

            try
            {
                // Don't interfere with back navigation or same-page navigation
                //if (e.Source == ShellNavigationSource.PopToRoot ||
                //    e.Source == ShellNavigationSource.Pop ||
                //    e.Current?.Location == e.Target?.Location)
                //{
                //    return;
                //}
                
                // Extract the route from the target
                string route = ExtractRouteFromUri(e.Target.Location);
                Debug.WriteLine($"Navigating to: {route}");

                // Simply update UI based on route type without authentication checks
                if (route == "login" )
                {
                    ShowLoginContent();
                }
                else
                {
                    ShowMainContent();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnNavigating: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        private string ExtractRouteFromUri(Uri uri)
        {
            if (uri == null) return string.Empty;

            string path = uri.OriginalString;

            // Remove any prefixes like "//" or "/"
            path = path.TrimStart('/');

            // Handle potential shell navigation format (//route)
            int routeStart = path.LastIndexOf('/');
            if (routeStart >= 0)
            {
                path = path.Substring(routeStart + 1);
            }

            return path;
        }

        
        private async Task InitializeAppAndCheckAuth()
        {
            try
            {
                // Initialize database first
                await InitializeDatabaseAsync();

                // Then check authentication and update UI
                await CheckAuthAndUpdateUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during app initialization: {ex.Message}");
                // Default to showing login on error
                ShowLoginContent();
            }
        }

        private async Task CheckAuthAndUpdateUI()
        {
            try
            {
                // Check if auth service is available
                if (_authService == null)
                {
                    Debug.WriteLine("Error: AuthService is null");
                    ShowLoginContent();
                    return;
                }

                // Check authentication status
                bool isAuthenticated = await _authService.IsAuthenticated();

                if (!isAuthenticated)
                {
                    Debug.WriteLine("No authenticated user. Showing login page...");
                    ShowLoginContent();
                }
                else
                {
                    // User is authenticated, show main content
                    Debug.WriteLine("User already authenticated. Showing main content...");
                    ShowMainContent();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during authentication check: {ex.Message}");
                ShowLoginContent();
            }
        }

        public void ShowLoginContent()
        {
            //if (LoginContent.IsVisible) return;

            LoginContent.IsVisible = true;
            MainTabs.IsVisible = false;
            CurrentItem = LoginContent;
        }

        public void ShowMainContent()
        {
            //if (MainTabs.IsVisible) return;

            LoginContent.IsVisible = false;
            MainTabs.IsVisible = true;
            CurrentItem = MainTabs;
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
#if DEBUG
                bool needsSeeding = await _databaseService.CheckIfDatabaseNeedsSeedingAsync();

                if (needsSeeding)
                {
                    Debug.WriteLine("Database needs seeding, initializing...");
                    await _databaseService.SeedDatabaseIfNeeded();
                    Debug.WriteLine("Database initialized successfully!");
                }
                else
                {
                    Debug.WriteLine("Database is already populated, seeding not necessary.");
                }
#else
                await _databaseService.InitializeDatabaseAsync();
#endif
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing database: {ex.Message}");
                throw;
            }
        }

       

        // Implement IDisposable pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from events
                    Navigating -= OnNavigating;
                }

                _disposed = true;
            }
        }
    }
}