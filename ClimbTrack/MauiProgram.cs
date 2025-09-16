using ClimbTrack.Converters;
using ClimbTrack.Services;
using ClimbTrack.ViewModels;
using ClimbTrack.Views;
using Microcharts.Maui;
using Microsoft.Extensions.Logging;

namespace ClimbTrack
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                 .UseMicrocharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Nel MauiProgram.cs
            builder.Services.AddSingleton<IEmailService>(serviceProvider =>
                 new SmtpEmailService(
                     "smtp.gmail.com",
                     587,
                     "koubaaachraf99@gmail.com",
                     "zcws lduo gbbz ened", // Per Gmail, dovrai generare una "App Password"
                     "koubaaachraf99@gmail.com",
                     "ClimbTrack",
                     true
                 )
             );

            // Test services
            builder.Services.AddSingleton<IEmailService, MockEmailService>();

            // Register core services first
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<ILocalHttpServer, LocalHttpServer>();

            // Register Firebase services
            builder.Services.AddSingleton<IServiceAccountService, ServiceAccountService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
            
            // Register domain services
            builder.Services.AddSingleton<IClimbingService, ClimbingService>();
            builder.Services.AddSingleton<ITrainingService, TrainingService>();
            builder.Services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();
            builder.Services.AddSingleton<INetworkService, NetworkService>();
            builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
            builder.Services.AddSingleton<IDashboardService, DashboardService>();

            // Register the FirebaseService last (if still needed)
            builder.Services.AddScoped<IFirebaseService, FirebaseService>();

            // Register converters
            builder.Services.AddSingleton<StringNotNullOrEmptyBoolConverter>();
            builder.Services.AddSingleton<BoolToColorConverter>();
            builder.Services.AddSingleton<InverseBoolConverter>();
            builder.Services.AddSingleton<BoolToStyleConverter>();
            builder.Services.AddSingleton<DifficultyToColorConverter>();
            builder.Services.AddSingleton<ItalianDateConverter>();

            // Register converters in resources
            builder.Services.AddSingleton<ResourceDictionary>(provider =>
            {
                var dictionary = new ResourceDictionary();
                dictionary.Add("StringNotNullOrEmptyBoolConverter", provider.GetRequiredService<StringNotNullOrEmptyBoolConverter>());
                dictionary.Add("BoolToColorConverter", provider.GetRequiredService<BoolToColorConverter>());
                dictionary.Add("InverseBoolConverter", provider.GetRequiredService<InverseBoolConverter>());
                dictionary.Add("BoolToStyleConverter", provider.GetRequiredService<BoolToStyleConverter>());
                dictionary.Add("DifficultyToColorConverter", provider.GetRequiredService<DifficultyToColorConverter>());
                dictionary.Add("ItalianDateConverter", provider.GetRequiredService<ItalianDateConverter>());
                return dictionary;
            });

            // Register view models
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<HomeViewModel>();
            builder.Services.AddTransient<TrainingViewModel>();
            builder.Services.AddTransient<ProfileViewModel>();
            builder.Services.AddTransient<EditProfileViewModel>();
            builder.Services.AddTransient<SessionDetailsViewModel>();
            builder.Services.AddTransient<AdminViewModel>();
            builder.Services.AddTransient<HistoricalViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();


            // Register pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<HomePage>();
            builder.Services.AddTransient<TrainingPage>();
            builder.Services.AddTransient<ProfilePage>();
            builder.Services.AddTransient<EditProfilePage>();
            builder.Services.AddTransient<SessionDetailsPage>();
            builder.Services.AddTransient<AdminPage>();
            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddTransient<HistoricalPage>();
            builder.Services.AddTransient<DashboardPage>();

            // Add logging
            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddDebug();
                // You can add other logging providers here
            });

            // Set up global exception handling
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                var errorService = builder.Services.BuildServiceProvider().GetService<IErrorHandlingService>();
                if (errorService != null)
                {
                    errorService.HandleExceptionAsync((Exception)args.ExceptionObject, "UnhandledException", true).Wait();
                }
            };

            TaskScheduler.UnobservedTaskException += (sender, args) =>
            {
                args.SetObserved(); // Prevent the app from crashing
                var errorService = builder.Services.BuildServiceProvider().GetService<IErrorHandlingService>();
                if (errorService != null)
                {
                    errorService.HandleExceptionAsync(args.Exception, "UnobservedTaskException", true).Wait();
                }
            };

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
