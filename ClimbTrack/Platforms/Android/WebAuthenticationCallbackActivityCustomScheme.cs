using Android.App;
using Android.Content;
using Android.Content.PM;

namespace ClimbTrack.Platforms.Android
{
    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
    [IntentFilter(new[] { Intent.ActionView },
                  Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
                  DataScheme = "com.companyname.climbtrack",
                  DataHost = "oauth2redirect")]
    public class WebAuthenticationCallbackActivityCustomScheme : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
    {
    }
}
