using ClimbTrack.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly IFirebaseService _firebaseService;

        private string _name;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _errorMessage;
        private string _busyText;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

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

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
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

        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }
        public ICommand RegisterWithVerificationCommand { get; }

        public RegisterViewModel(
            IAuthService authService,
            IDatabaseService databaseService,
            IFirebaseService firebaseService)
        {
            Title = "Register";
            _authService = authService;
            _databaseService = databaseService;
            _firebaseService = firebaseService;

            RegisterCommand = new Command(async () => await ExecuteRegisterCommand());
            BackToLoginCommand = new Command(async () => await ExecuteBackToLoginCommand());
            RegisterWithVerificationCommand = new Command(async () => await ExecuteRegisterWithVerificationCommand());
            
        }

        private async Task ExecuteRegisterCommand()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    // Clear any previous error
                    ErrorMessage = string.Empty;

                    // Validate inputs
                    if (string.IsNullOrWhiteSpace(Name) ||
                        string.IsNullOrWhiteSpace(Email) ||
                        string.IsNullOrWhiteSpace(Password) ||
                        string.IsNullOrWhiteSpace(ConfirmPassword))
                    {
                        ErrorMessage = "Please fill in all fields";
                        return;
                    }

                    // Validate email format
                    if (!IsValidEmail(Email))
                    {
                        ErrorMessage = "Please enter a valid email address";
                        return;
                    }

                    if (Password != ConfirmPassword)
                    {
                        ErrorMessage = "Passwords do not match";
                        return;
                    }

                    // Validate password strength
                    //if (!IsPasswordStrong(Password))
                    //{
                    //    ErrorMessage = "Password must be at least 8 characters long and include uppercase, lowercase, number, and special character";
                    //    return;
                    //}

                    // Update busy text
                    BusyText = "Creating your account...";

                    // Register user with Firebase
                    var userCredential = await _firebaseService.SignUpWithEmailAndPassword(Email, Password);
                    await _authService.SaveAuthData(userCredential);

                    // Create user profile in database
                    var userId = userCredential.User.Uid;
                    var userData = new Dictionary<string, object>
                    {
                        { "Id", userId },
                        { "Name", Name },
                        { "Email", Email },
                        { "JoinDate", DateTime.UtcNow.ToString("o") },
                        { "EmailVerified", false }
                    };

                    await _databaseService.UpdateItem("users", userId, userData);

                    // Send verification email
                    try
                    {
                        BusyText = "Sending verification email...";
                        await _firebaseService.SendVerificationCodeEmailAsync(Email);

                        // Prompt user to verify email now
                        bool verifyNow = await Shell.Current.DisplayAlert(
                            "Email Verification",
                            "A verification code has been sent to your email. Would you like to verify your email now?",
                            "Yes", "Later");

                        if (verifyNow)
                        {
                            string code = await Shell.Current.DisplayPromptAsync(
                                "Verification",
                                "Enter the code sent to your email");

                            if (!string.IsNullOrWhiteSpace(code))
                            {
                                BusyText = "Verifying email...";
                                await _firebaseService.VerifyCodeAndSignInAsync(Email, code);

                                // Update user profile to mark as verified
                                await _databaseService.UpdateItem("users", userId,
                                    new Dictionary<string, object> { { "EmailVerified", true } });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Non-critical, so just log it
                        Console.WriteLine($"Failed to send verification email: {ex.Message}");
                    }

                    await Shell.Current.DisplayAlert("Success", "Registration successful", "OK");
                    await Shell.Current.GoToAsync("///home");
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("email-already-in-use") ||
                        ex.Message.Contains("EMAIL_EXISTS"))
                    {
                        ErrorMessage = "This email is already registered. Please use a different email or try to login.";
                    }
                    else if (ex.Message.Contains("weak-password"))
                    {
                        ErrorMessage = "Password is too weak. Please choose a stronger password.";
                    }
                    else
                    {
                        ErrorMessage = $"Registration failed: {ex.Message}";
                    }
                }
            });
        }

        private async Task ExecuteRegisterWithVerificationCommand()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    // Clear any previous error
                    ErrorMessage = string.Empty;

                    // Validate name and email
                    if (string.IsNullOrWhiteSpace(Name) ||
                        string.IsNullOrWhiteSpace(Email))
                    {
                        ErrorMessage = "Please enter your name and email";
                        return;
                    }

                    // Validate email format
                    if (!IsValidEmail(Email))
                    {
                        ErrorMessage = "Please enter a valid email address";
                        return;
                    }

                    // Send verification code
                    BusyText = "Sending verification code...";
                    await _firebaseService.SendVerificationCodeEmailAsync(Email);

                    // Prompt for verification code
                    string code = await Shell.Current.DisplayPromptAsync(
                        "Verification",
                        "Enter the code sent to your email");

                    if (string.IsNullOrWhiteSpace(code))
                    {
                        return; // User cancelled
                    }

                    // Verify code and create account
                    BusyText = "Verifying and creating account...";
                    var userCredential = await _firebaseService.VerifyCodeAndSignInAsync(Email, code);
                    await _authService.SaveAuthData(userCredential);

                    // Create user profile
                    var userId = userCredential.User.Uid;
                    var userData = new Dictionary<string, object>
                    {
                        { "Id", userId },
                        { "Name", Name },
                        { "Email", Email },
                        { "JoinDate", DateTime.UtcNow.ToString("o") },
                        { "EmailVerified", true }
                    };

                    await _databaseService.UpdateItem("users", userId, userData);

                    await Shell.Current.DisplayAlert("Success", "Registration successful", "OK");
                    await Shell.Current.GoToAsync("///home");
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Registration failed: {ex.Message}";
                }
            });
        }

        private async Task ExecuteBackToLoginCommand()
        {
            await Shell.Current.GoToAsync("..");
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

        // Helper method to validate password strength
        private bool IsPasswordStrong(string password)
        {
            // At least 8 characters
            if (password.Length < 8)
                return false;

            // Check for uppercase
            if (!password.Any(char.IsUpper))
                return false;

            // Check for lowercase
            if (!password.Any(char.IsLower))
                return false;

            // Check for digit
            if (!password.Any(char.IsDigit))
                return false;

            // Check for special character
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
                return false;

            return true;
        }
    }
}