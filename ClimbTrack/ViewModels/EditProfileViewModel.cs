using ClimbTrack.Models;
using ClimbTrack.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    [QueryProperty(nameof(ProfileParameter), "Profile")]
    public class EditProfileViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        private UserProfile _userProfile;

        public UserProfile UserProfile
        {
            get => _userProfile;
            set => SetProperty(ref _userProfile, value);
        }

        public object ProfileParameter
        {
            set
            {
                if (value is UserProfile profile)
                {
                    UserProfile = profile;
                }
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditProfileViewModel(
            IDatabaseService databaseService,
            IAuthService authService,
            INavigationService navigationService)
        {
            _databaseService = databaseService;
            _authService = authService;
            _navigationService = navigationService;

            Title = "Modifica Profilo";
            UserProfile = new UserProfile();

            SaveCommand = new Command(async () => await Save());
            CancelCommand = new Command(async () => await _navigationService.GoBackAsync());
        }

        private async Task Save()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    string userId = await _authService.GetUserId();
                    await _databaseService.UpdateItem($"users/{userId}", "profile", UserProfile);
                    await Application.Current.MainPage.DisplayAlert("Successo", "Profilo aggiornato con successo!", "OK");
                    await _navigationService.GoBackAsync();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Errore", $"Impossibile aggiornare il profilo: {ex.Message}", "OK");
                }
            });
        }
    }
}