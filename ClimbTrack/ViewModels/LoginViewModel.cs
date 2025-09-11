using ClimbTrack.Services;
using System.Diagnostics;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IDatabaseService _databaseService;
        private readonly IFirebaseService _firebaseService;

        private string _email;
        private string _password;
        private string _errorMessage;
        private string _busyText;

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

        public string BusyText
        {
            get => _busyText;
            set => SetProperty(ref _busyText, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand GoogleSignInCommand { get; }
        public ICommand EmailVerificationCommand { get; }

        public LoginViewModel(
            IAuthService authService,
            INavigationService navigationService,
            IGoogleAuthService googleAuthService,
            IDatabaseService databaseService,
            IFirebaseService firebaseService)
        {
            Title = "Login";
            _authService = authService;
            _navigationService = navigationService;
            _googleAuthService = googleAuthService;
            _databaseService = databaseService;
            _firebaseService = firebaseService;

            LoginCommand = new Command(async () => await ExecuteLoginCommand());
            RegisterCommand = new Command(async () => await ExecuteRegisterCommand());
            ForgotPasswordCommand = new Command(async () => await ExecuteForgotPasswordCommand());
            GoogleSignInCommand = new Command(async () => await ExecuteGoogleSignInCommand());
            EmailVerificationCommand = new Command(async () => await ExecuteEmailVerificationCommand());
            
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

                    await _databaseService.UpdateItem("users", userId, userData);

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
                    // Clear any previous error
                    ErrorMessage = string.Empty;

                    if (string.IsNullOrWhiteSpace(Email))
                    {
                        ErrorMessage = "Please enter your email";
                        return;
                    }

                    if (!IsValidEmail(Email))
                    {
                        ErrorMessage = "Please enter a valid email address";
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(Password))
                    {
                        ErrorMessage = "Please enter your password";
                        return;
                    }

                    // Update busy text
                    BusyText = "Signing in...";

                    var userCredential = await _firebaseService.SignInWithEmailAndPassword(Email, Password);
                    await _authService.SaveAuthData(userCredential);

                    // Navigate to main page
                    await _navigationService.NavigateToMainPage();
                }

                catch (Exception ex)
                {
                    // Handle other general exceptions
                    Debug.WriteLine($"Login error: {ex}");
                    ErrorMessage = "Login failed. Please try again later.";
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

        private async Task ExecuteEmailVerificationCommand()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    // Clear any previous error
                    ErrorMessage = string.Empty;

                    string email = await _navigationService.DisplayPromptAsync("Email Verification", "Enter your email address");

                    if (string.IsNullOrWhiteSpace(email))
                    {
                        return; // User cancelled
                    }

                    // Validate email format
                    if (!IsValidEmail(email))
                    {
                        ErrorMessage = "Please enter a valid email address";
                        return;
                    }

                    // Update busy text
                    BusyText = "Sending verification code...";

                    // Send verification code
                    //_firawait _firebaseService.SendVerificationCodeEmailAsync(email);

                    // Prompt for verification code
                    string code = await _navigationService.DisplayPromptAsync("Verification", "Enter the code sent to your email");

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        return; // User cancelled
                    }

                    // Update busy text
                    BusyText = "Verifying code...";

                    // Verify code and sign in
                    var userCredential = await _firebaseService.VerifyCodeAndSignInAsync(email, code);
                    await _authService.SaveAuthData(userCredential);

                    // Create or update user profile
                    var userId = userCredential.User.Uid;
                    var userData = new Dictionary<string, object>
                    {
                        { "Id", userId },
                        { "Email", email },
                        { "LastLogin", DateTime.UtcNow.ToString("o") }
                    };

                    await _databaseService.UpdateItem("users", userId, userData);

                    // Navigate to main page
                    await _navigationService.NavigateToMainPage();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Email verification failed: {ex.Message}";
                    Console.WriteLine($"Email verification failed: {ex.Message}");
                }
            });
        }

        // Helper method to validate email format
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}