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
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;

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
        public ICommand NavigateToProfileCommand { get; }
        public ICommand NavigateToTrainingCommand { get; }
        public ICommand NavigateToHistoryCommand { get; }

        public MainViewModel(
            IDatabaseService databaseService,
            IAuthService authService)
        {
            Title = "Home";
            _databaseService = databaseService;
            _authService = authService;

            LogoutCommand = new Command(async () => await ExecuteLogoutCommand());
            NavigateToProfileCommand = new Command(async () => await Shell.Current.GoToAsync("//profile"));
            NavigateToTrainingCommand = new Command(async () => await Shell.Current.GoToAsync("//training"));
            NavigateToHistoryCommand = new Command(async () => await Shell.Current.GoToAsync("//history"));
        }

        public async Task Initialize()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    // Verifica se l'utente è ancora autenticato
                    bool isAuthenticated = await _authService.IsAuthenticated();
                    if (!isAuthenticated)
                    {
                        await Shell.Current.GoToAsync("///LoginPage");
                        return;
                    }

                    UserId = await _authService.GetUserId();
                    UserEmail = await _authService.GetUserEmail();

                    // Get user data from database
                    var userData = await _databaseService.GetItem<Dictionary<string, object>>("users", UserId);

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
                    await Shell.Current.DisplayAlert("Error", $"Failed to load user data: {ex.Message}", "OK");
                }
            });
        }

        private async Task ExecuteLogoutCommand()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Logout",
                "Sei sicuro di voler effettuare il logout?",
                "Sì", "No");

            if (confirm)
            {
                try
                {
                    _authService.Logout();
                    await Shell.Current.GoToAsync("///login");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Errore", $"Impossibile effettuare il logout: {ex.Message}", "OK");
                }
            }
        }
    }
}