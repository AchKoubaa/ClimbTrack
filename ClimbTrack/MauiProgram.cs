using ClimbTrack.Converters;
using ClimbTrack.Services;
using ClimbTrack.ViewModels;
using ClimbTrack.Views;
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
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register services
            builder.Services.AddSingleton<IFirebaseService, FirebaseService>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IGoogleAuthService, GoogleAuthService>();

            // Register view models
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<RegisterViewModel>();
            builder.Services.AddTransient<MainViewModel>();

            // Register pages
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<RegisterPage>();
            builder.Services.AddTransient<MainPage>();

            // Register converters in resources
            builder.Services.AddSingleton<ResourceDictionary>(provider => {
                var dictionary = new ResourceDictionary();
                dictionary.Add("StringNotNullOrEmptyBoolConverter", new StringNotNullOrEmptyBoolConverter());
                return dictionary;
            });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }

       
      }
}
