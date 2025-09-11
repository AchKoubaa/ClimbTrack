using ClimbTrack.Services;

namespace ClimbTrack.Views;

public partial class GoogleSignInTestPage : ContentPage
{
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IAuthService _authService;

    public GoogleSignInTestPage(IGoogleAuthService googleAuthService, IAuthService authService)
    {
        InitializeComponent();
        _googleAuthService = googleAuthService;
        _authService = authService;
    }

    private async void OnGoogleSignInClicked(object sender, EventArgs e)
    {
        try
        {
            ResultLabel.Text = "Signing in with Google...";
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var userCredential = await _googleAuthService.SignInWithGoogleAsync();

            if (userCredential != null)
            {
                await _authService.SaveAuthData(userCredential);

                ResultLabel.Text = $"Sign in successful!\n" +
                                  $"User ID: {userCredential.User.Uid}\n" +
                                  $"Email: {userCredential.User.Info.Email}\n" +
                                  $"Name: {userCredential.User.Info.DisplayName}";
            }
        }
        catch (Exception ex)
        {
            ResultLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }
}