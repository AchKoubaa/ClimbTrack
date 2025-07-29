using ClimbTrack.Services;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IFirebaseService _firebaseService;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IGoogleAuthService _googleAuthService;

        private string _email;
        private string _password;
        private string _errorMessage;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand GoogleSignInCommand { get; }

        public LoginViewModel(
            IFirebaseService firebaseService, 
            IAuthService authService, 
            INavigationService navigationService,
            IGoogleAuthService googleAuthService)
        {
            Title = "Login";
            _firebaseService = firebaseService;
            _authService = authService;
            _navigationService = navigationService;
            _googleAuthService = googleAuthService;

            LoginCommand = new Command(async () => await ExecuteLoginCommand());
            RegisterCommand = new Command(async () => await ExecuteRegisterCommand());
            ForgotPasswordCommand = new Command(async () => await ExecuteForgotPasswordCommand());
            GoogleSignInCommand = new Command(async () => await ExecuteGoogleSignInCommand());
        }

        private async Task ExecuteGoogleSignInCommand()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    ErrorMessage = string.Empty;

                    // Sign in with Google
                    var userCredential = await _googleAuthService.SignInWithGoogleAsync();

                    // Save auth data
                    await _authService.SaveAuthData(userCredential);

                    // Create or update user profile
                    var userId = userCredential.User.Uid;
                    var userData = new Dictionary<string, object>
                {
                    { "Id", userId },
                    { "Name", userCredential.User.Info.DisplayName ?? "Google User" },
                    { "Email", userCredential.User.Info.Email },
                    { "PhotoUrl", userCredential.User.Info.PhotoUrl?.ToString() ?? "" },
                    { "LastLogin", DateTime.UtcNow.ToString("o") }
                };

                    await _firebaseService.UpdateItem("users", userId, userData);

                    // Navigate to main page
                    await _navigationService.NavigateToMainPage();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Google Sign-In failed: {ex.Message}";
                }
            });
        }
        private async Task ExecuteLoginCommand()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
                    {
                        ErrorMessage = "Please enter email and password";
                        return;
                    }

                    var userCredential = await _firebaseService.SignInWithEmailAndPassword(Email, Password);
                    await _authService.SaveAuthData(userCredential);

                    // Navigate to main page
                    await _navigationService.NavigateToMainPage();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Login failed: {ex.Message}";
                }
            });
        }

        private async Task ExecuteRegisterCommand()
        {
            await _navigationService.NavigateToAsync("RegisterPage");
        }

        private async Task ExecuteForgotPasswordCommand()
        {
            string email = await _navigationService.DisplayPromptAsync("Password Reset", "Enter your email address");

            if (!string.IsNullOrWhiteSpace(email))
            {
                await ExecuteWithBusy(async () =>
                {
                    try
                    {
                        await _firebaseService.ResetPassword(email);
                        await _navigationService.DisplayAlertAsync("Success", "Password reset email sent", "OK");
                    }
                    catch (Exception ex)
                    {
                        await _navigationService.DisplayAlertAsync("Error", ex.Message, "OK");
                    }
                });
            }
        }

        public async Task CheckAuthenticationStatus()
        {
            if (await _authService.IsAuthenticated())
            {
                await _navigationService.NavigateToMainPage();
            }
        }
    }
}
