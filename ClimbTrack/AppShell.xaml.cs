using ClimbTrack.Views;
using System.Diagnostics;

namespace ClimbTrack
{
    public partial class AppShell : Shell, IDisposable
    {
        private bool _disposed = false;
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            RegisterRoutes();

            //// Initially hide the Storico tab
            if (StoricoTab != null)
            {
                StoricoTab.IsVisible = true;
            }

            // Subscribe to navigation events
            Navigated += OnNavigated;
        }

        private void RegisterRoutes()
        {
            // Register main pages
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
        }

        private void OnNavigated(object sender, ShellNavigatedEventArgs e)
        {
            try
            {
                if (StoricoTab == null) return;

                // Get the current route
                var route = e.Current?.Location?.OriginalString;
                if (string.IsNullOrEmpty(route)) return;

                // Hide Storico tab on HomePage, show it on other pages
                bool isHomePage = route.EndsWith("/home") || route.EndsWith("/main/home");
                bool isTrainingPage = route.Contains("/training") || route.Contains("/main/training");
                bool isProfilePage = route.Contains("/profile") || route.Contains("/main/profile");

                // Update Home tab visibility
                if (isHomePage)
                {
                    // On Home page: Show Home, Training, Profile
                    HomeTab.IsVisible = true;
                    DashboardTab.IsVisible = false;
                    TrainingTab.IsVisible = true;
                    ProfileTab.IsVisible = true;
                    StoricoTab.IsVisible = false;
                }
                else if (isTrainingPage)
                {
                    // On Training page: Show Dashboard, Training, Profile, Storico
                    HomeTab.IsVisible = false;
                    DashboardTab.IsVisible = true;
                    TrainingTab.IsVisible = true;
                    ProfileTab.IsVisible = true;
                    StoricoTab.IsVisible = true;
                }
                else if (isProfilePage)
                {
                    // On Profile page: Show Home, Training, Profile, Storico
                    HomeTab.IsVisible = true;
                    DashboardTab.IsVisible = false;
                    TrainingTab.IsVisible = true;
                    ProfileTab.IsVisible = true;
                    StoricoTab.IsVisible = true;
                }
                else
                {
                    // Default: Show all tabs
                    HomeTab.IsVisible = true;
                    DashboardTab.IsVisible = true;
                    TrainingTab.IsVisible = true;
                    ProfileTab.IsVisible = true;
                    StoricoTab.IsVisible = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnNavigated: {ex.Message}");
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
                    // Unsubscribe from events to prevent memory leaks
                    Navigated -= OnNavigated;

                    // Dispose any other managed resources here
                }

                _disposed = true;
            }
        }

    }
}