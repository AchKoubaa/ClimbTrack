using ClimbTrack.Services;
using ClimbTrack.Views;
using System.Diagnostics;

namespace ClimbTrack
{
    public partial class AppShell : Shell
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private bool _isInitialized = false;
        public AppShell(IAuthService authService, IDatabaseService databaseService)
        {
            InitializeComponent();
            _authService = authService;
            _databaseService = databaseService;

            // Register routes for navigation
            RegisterRoutes();

            // Subscribe to Appearing event
            Appearing += OnAppearing;
        }

        private void OnAppearing(object sender, EventArgs e)
        {
            if (!_isInitialized)
            {
                _isInitialized = true;

                // Now Shell.Current should be available
                _ = InitializeAppAndCheckAuth();
            }
        }
        private void RegisterRoutes()
        {
            try
            {
                Routing.RegisterRoute("register", typeof(RegisterPage));
                Routing.RegisterRoute("editProfile", typeof(EditProfilePage));
                Routing.RegisterRoute("sessionDetails", typeof(SessionDetailsPage));
                Routing.RegisterRoute("admin", typeof(AdminPage));

                Debug.WriteLine("Routes registered successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error registering routes: {ex.Message}");
            }
        }

        private async Task InitializeAppAndCheckAuth()
        {
            try
            {
                // Initialize database first
              //  await InitializeDatabaseAsync();

                // Then check authentication and update UI
                await CheckAuthAndUpdateUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during app initialization: {ex.Message}");
                // Default to showing login on error
                await Shell.Current.GoToAsync("///login");
            }
        }

        private async Task CheckAuthAndUpdateUI()
        {
            try
            {
                // Check if Shell.Current is available
                if (Shell.Current == null)
                {
                    Debug.WriteLine("Error: Shell.Current is null in CheckAuthAndUpdateUI");
                    return;
                }
                if (Shell.Current != null)
                {
                    // Check if user is authenticated
                    bool isAuthenticated = await _authService.IsAuthenticated();

                    // Navigate to appropriate starting page
                    if (isAuthenticated)
                    {
                        await Shell.Current.GoToAsync("///home");
                    }
                    else
                    {
                        await Shell.Current?.GoToAsync("///login");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during authentication check: {ex.Message}");
                await Shell.Current.GoToAsync("///login");
            }
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
    }
}