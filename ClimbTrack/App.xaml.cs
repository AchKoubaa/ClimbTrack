using ClimbTrack.Services;
using System.Diagnostics;

namespace ClimbTrack
{
    public partial class App : Application
    {
        
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly AppShell _appShell;

        public App(AppShell appShell,
            IErrorHandlingService errorHandlingService)
        {
            InitializeComponent();
            _appShell = appShell;
            _errorHandlingService = errorHandlingService;

           
            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Use the injected AppShell instance
            return new Window(_appShell);
        }

        protected override void OnStart()
        {
            base.OnStart();
            // AppShell now handles database initialization and navigation
        }

        protected override void OnSleep()
        {
            base.OnSleep();
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            base.OnResume();
            // Handle when your app resumes
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            // Safely cast the exception object
            var exception = e.ExceptionObject as Exception;

            // Log the exception if it's not null
            if (exception != null)
            {
                LogException(exception, "Unhandled AppDomain Exception");
            }
            else
            {
                // Simple fallback for non-Exception objects
                Debug.WriteLine($"CRITICAL ERROR: Unhandled non-Exception object");
            }
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception, "Unobserved Task Exception");
            e.SetObserved(); // Prevents the app from closing
        }

        private void LogException(Exception exception, string source)
        {
            if (exception == null) return;

            Debug.WriteLine($"CRITICAL ERROR ({source}): {exception.Message}");
            Debug.WriteLine($"Stack Trace: {exception.StackTrace}");

            // You can add logic here to send the error to a logging service
            _errorHandlingService?.HandleExceptionAsync(exception, source, false).ConfigureAwait(false);
        }
    }
}