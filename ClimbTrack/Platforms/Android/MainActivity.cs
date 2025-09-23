using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace ClimbTrack
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Initialize Firebase if needed
            // For example, you might want to initialize Firebase Analytics or Messaging here

            // You can also add any Android-specific initialization code here

            // For debugging purposes, you can log the package name
            Console.WriteLine($"Android package name: {PackageName}");
        }

        // If you need to handle activity results (for example, from Google Sign-In)
        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            // You can add custom handling for activity results here if needed
            Console.WriteLine($"OnActivityResult: requestCode={requestCode}, resultCode={resultCode}");
        }

        // Handle back button presses if needed
        public override void OnBackPressed()
        {
            // You can add custom back button handling here if needed
            base.OnBackPressed();
        }

    }
}
