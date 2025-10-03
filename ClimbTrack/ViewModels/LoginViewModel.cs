using ClimbTrack.Services;
using ClimbTrack.Views;
using System.Diagnostics;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
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
            IGoogleAuthService googleAuthService,
            IDatabaseService databaseService,
            IFirebaseService firebaseService)
        {
            Title = "Login";
            _authService = authService;
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
                    await Shell.Current.GoToAsync("///home");
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
                    // Validate inputs with DisplayAlert instead of ErrorMessage
                    if (string.IsNullOrWhiteSpace(Email))
                    {
                        await Shell.Current.DisplayAlert("Login Error", "Please enter your email", "OK");
                        return;
                    }

                    if (!IsValidEmail(Email))
                    {
                        await Shell.Current.DisplayAlert("Login Error", "Please enter a valid email address", "OK");
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(Password))
                    {
                        await Shell.Current.DisplayAlert("Login Error", "Please enter your password", "OK");
                        return;
                    }

                    // Update busy text
                    BusyText = "Signing in...";

                    // Call the authentication service
                    var response = await _firebaseService.SignInWithEmailAndPassword(Email, Password);

                    // Handle the response
                    if (response.Flag)
                    {
                        // Success case
                        await Shell.Current.GoToAsync("///home");
                    }
                    else
                    {
                        // Error case - display the error message in an alert
                        string title = "Login Failed";
                        string message = response.Message;

                        // Show alert with the error message from the service
                        await Shell.Current.DisplayAlert(title, message, "OK");
                    }
                }
                catch (Exception ex)
                {
                    // This should rarely happen now since most errors are handled in the service
                    Debug.WriteLine($"Unhandled login error: {ex}");

                    // Show a generic error alert for unexpected exceptions
                    await Shell.Current.DisplayAlert(
                        "Unexpected Error",
                        "An unexpected error occurred. Please try again later.",
                        "OK");
                    
                }
            });
        }

        private async Task ExecuteRegisterCommand()
        {
          
            await Shell.Current.GoToAsync("register");
        }

        private async Task ExecuteForgotPasswordCommand()
        {
            string email = await Shell.Current.DisplayPromptAsync("Password Reset", "Enter your email address");

            if (!string.IsNullOrWhiteSpace(email))
            {
                await ExecuteWithBusy(async () =>
                {
                    try
                    {
                        await _firebaseService.ResetPassword(email);
                        await Shell.Current.DisplayAlert("Success", "Password reset email sent", "OK");
                    }
                    catch (Exception ex)
                    {
                        await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
                    }
                });
            }
        }

        private bool _isCheckingAuth = false;
        public async Task CheckAuthenticationStatus()
        {
            if (_isCheckingAuth) return;

            try
            {
                _isCheckingAuth = true;
                if (await _authService.IsAuthenticated())
                {
                    await Shell.Current.GoToAsync("///home");
                }
            }
            finally
            {
                _isCheckingAuth = false;
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

                    string email = await Shell.Current.DisplayPromptAsync("Email Verification", "Enter your email address");

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
                    string code = await Shell.Current.DisplayPromptAsync("Verification", "Enter the code sent to your email");

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
                    await Shell.Current.GoToAsync("///home");
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