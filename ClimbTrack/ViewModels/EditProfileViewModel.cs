using ClimbTrack.Models;
using ClimbTrack.Services;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    [QueryProperty(nameof(ProfileId), "ProfileId")]
    public class EditProfileViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;

        private UserProfile _userProfile;
        private string _profileId;

        public UserProfile UserProfile
        {
            get => _userProfile;
            set => SetProperty(ref _userProfile, value);
        }

        public string ProfileId
        {
            get => _profileId;
            set
            {
                _profileId = value;
                LoadProfileAsync(_profileId).ConfigureAwait(false);
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
            CancelCommand = new Command(async () => await GoBack());
        }

        private async Task LoadProfileAsync(string profileId)
        {
            if (string.IsNullOrEmpty(profileId))
            {
                return;
            }

            await ExecuteWithBusy(async () =>
            {
                try
                {
                    string userId = await _authService.GetUserId();
                    // Assuming you have a method to get a user profile by ID
                    UserProfile = await _databaseService.GetItem<UserProfile>($"users/{userId}", "profile");

                    if (UserProfile == null)
                    {
                        UserProfile = new UserProfile();
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Errore", $"Impossibile caricare il profilo: {ex.Message}", "OK");
                    UserProfile = new UserProfile();
                }
            });
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
                    await GoBack();
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Errore", $"Impossibile aggiornare il profilo: {ex.Message}", "OK");
                }
            });
        }

        private async Task GoBack()
        {
            try
            {
                // Replace "//profile" with your actual route to the profile page
                await Shell.Current.GoToAsync("//profile");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Route navigation failed: {ex.Message}");
            }
        }
    }
}