using ClimbTrack.Converters;
using ClimbTrack.Services;
using ClimbTrack.ViewModels;
using ClimbTrack.Views;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
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
                .UseMauiCommunityToolkit()
                .UseMicrocharts()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder
                .RegisterServices()
                .RegisterConverters()
                .RegisterViewModels()
                .RegisterViews()
                .ConfigureLogging();

            return builder.Build();
        }

        public static MauiAppBuilder RegisterServices(this MauiAppBuilder builder)
        {
            // Email services
            builder.Services.AddSingleton<IEmailService>(serviceProvider =>
                new SmtpEmailService(
                    "smtp.gmail.com",
                    587,
                    "koubaaachraf99@gmail.com",
                    "zcws lduo gbbz ened", // For Gmail, you'll need to generate an "App Password"
                    "koubaaachraf99@gmail.com",
                    "ClimbTrack",
                    true
                )
            );

            // Test services
            //builder.Services.AddSingleton<IEmailService, MockEmailService>();

            // Core services
            builder.Services.AddSingleton<ILocalHttpServer, LocalHttpServer>();

            // Firebase services
            builder.Services.AddSingleton<IServiceAccountService, ServiceAccountService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();
            builder.Services.AddScoped<IFirebaseService, FirebaseService>();

            // Domain services
            builder.Services.AddSingleton<IClimbingService, ClimbingService>();
            builder.Services.AddSingleton<ITrainingService, TrainingService>();
            builder.Services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            builder.Services.AddSingleton<IAnalyticsService, AnalyticsService>();
            builder.Services.AddSingleton<INetworkService, NetworkService>();
            builder.Services.AddSingleton<IConnectivity>(Connectivity.Current);
            builder.Services.AddSingleton<IDashboardService, DashboardService>();
            builder.Services.AddSingleton<IPhotoPickerService, PhotoPickerService>();

            return builder;
        }

        public static MauiAppBuilder RegisterConverters(this MauiAppBuilder builder)
        {
            // Register converters
            builder.Services.AddSingleton<StringNotNullOrEmptyBoolConverter>();
            builder.Services.AddSingleton<BoolToColorConverter>();
            builder.Services.AddSingleton<InverseBoolConverter>();
            builder.Services.AddSingleton<BoolToStyleConverter>();
            builder.Services.AddSingleton<DifficultyToColorConverter>();
            builder.Services.AddSingleton<ItalianDateConverter>();
            builder.Services.AddSingleton<BoolToOpacityConverter>();

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
                dictionary.Add("BoolToOpacityConverter", provider.GetRequiredService<BoolToOpacityConverter>());
                return dictionary;
            });

            return builder;
        }

        public static MauiAppBuilder RegisterViewModels(this MauiAppBuilder builder)
        {
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

            return builder;
        }

        public static MauiAppBuilder RegisterViews(this MauiAppBuilder builder)
        {
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

            return builder;
        }

        public static MauiAppBuilder ConfigureLogging(this MauiAppBuilder builder)
        {
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

            return builder;
        }
    }
}