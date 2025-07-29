using ClimbTrack.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly IFirebaseService _firebaseService;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        private string _userId;
        private string _userEmail;
        private string _welcomeMessage;

        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        public string UserEmail
        {
            get => _userEmail;
            set => SetProperty(ref _userEmail, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public ICommand LogoutCommand { get; }

        public MainViewModel(IFirebaseService firebaseService, IAuthService authService, INavigationService navigationService)
        {
            Title = "Home";
            _firebaseService = firebaseService;
            _authService = authService;
            _navigationService = navigationService;

            LogoutCommand = new Command(async () => await ExecuteLogoutCommand());
        }

        public async Task Initialize()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    UserId = await _authService.GetUserId();
                    UserEmail = await _authService.GetUserEmail();

                    // Get user data from Firebase
                    var userData = await _firebaseService.GetItem<Dictionary<string, object>>("users", UserId);

                    if (userData != null && userData.ContainsKey("Name"))
                    {
                        string userName = userData["Name"].ToString();
                        WelcomeMessage = $"Hello, {userName}!";
                    }
                    else
                    {
                        WelcomeMessage = $"Hello, {UserEmail}!";
                    }
                }
                catch (Exception ex)
                {
                    await _navigationService.DisplayAlertAsync("Error", $"Failed to load user data: {ex.Message}", "OK");
                }
            });
        }

        private async Task ExecuteLogoutCommand()
        {
            await ExecuteWithBusy(async () =>
            {
                bool confirm = await _navigationService.DisplayAlertAsync(
                    "Logout",
                    "Are you sure you want to logout?",
                    "Yes",
                    "No");

                if (confirm)
                {
                    await _authService.Logout();

                    // Navigate back to login page
                    await _navigationService.NavigateToAsync("///LoginPage");
                }
            });
        }
    }
}
