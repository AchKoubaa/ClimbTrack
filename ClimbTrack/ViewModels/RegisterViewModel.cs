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
        private readonly IFirebaseService _firebaseService;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        private string _name;
        private string _email;
        private string _password;
        private string _confirmPassword;
        private string _errorMessage;

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

        public ICommand RegisterCommand { get; }
        public ICommand BackToLoginCommand { get; }

        public RegisterViewModel(IFirebaseService firebaseService, IAuthService authService, INavigationService navigationService)
        {
            Title = "Register";
            _firebaseService = firebaseService;
            _authService = authService;
            _navigationService = navigationService;

            RegisterCommand = new Command(async () => await ExecuteRegisterCommand());
            BackToLoginCommand = new Command(async () => await ExecuteBackToLoginCommand());
        }

        private async Task ExecuteRegisterCommand()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    // Validate inputs
                    if (string.IsNullOrWhiteSpace(Name) ||
                        string.IsNullOrWhiteSpace(Email) ||
                        string.IsNullOrWhiteSpace(Password) ||
                        string.IsNullOrWhiteSpace(ConfirmPassword))
                    {
                        ErrorMessage = "Please fill in all fields";
                        return;
                    }

                    if (Password != ConfirmPassword)
                    {
                        ErrorMessage = "Passwords do not match";
                        return;
                    }

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
                { "JoinDate", DateTime.UtcNow }
            };

                    await _firebaseService.UpdateItem("users", userId, userData);

                    await _navigationService.DisplayAlertAsync("Success", "Registration successful", "OK");
                    await _navigationService.NavigateToMainPage();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Registration failed: {ex.Message}";
                }
            });
        }

        private async Task ExecuteBackToLoginCommand()
        {
            await _navigationService.GoBackAsync();
        }
    }
}
