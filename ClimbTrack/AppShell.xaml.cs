namespace ClimbTrack
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for navigation
            Routing.RegisterRoute(nameof(Views.RegisterPage), typeof(Views.RegisterPage));
        }
    }
}
