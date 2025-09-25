using System;
using System.Threading.Tasks;
using System.Windows.Input;
using ClimbTrack.Models;
using ClimbTrack.Services;

namespace ClimbTrack.ViewModels
{
    [QueryProperty(nameof(ProfileId), "ProfileId")]
    public class EditProfileViewModel : BaseViewModel
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;
        private readonly INavigationService _navigationService;
        private readonly IPhotoPickerService _photoPickerService;

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
        public ICommand PickPhotoCommand { get; }

        public EditProfileViewModel(
            IDatabaseService databaseService,
            IAuthService authService,
            INavigationService navigationService,
            IPhotoPickerService photoPickerService)
        {
            _databaseService = databaseService;
            _authService = authService;
            _navigationService = navigationService;
            _photoPickerService = photoPickerService;

            Title = "Modifica Profilo";
            UserProfile = new UserProfile();

            SaveCommand = new Command(async () => await Save());
            CancelCommand = new Command(async () => await GoBack());
            PickPhotoCommand = new Command(async () => await PickPhoto());
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
                    UserProfile = await _databaseService.GetItem<UserProfile>($"users/{userId}", "profile");

                    if (UserProfile == null)
                    {
                        UserProfile = new UserProfile();
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Errore", $"Impossibile caricare il profilo: {ex.Message}", "OK");
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
                    await Shell.Current.DisplayAlert("Successo", "Profilo aggiornato con successo!", "OK");
                    await GoBack();
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Errore", $"Impossibile aggiornare il profilo: {ex.Message}", "OK");
                }
            });
        }

        private async Task PickPhoto()
        {
            if (IsBusy)
                return;

            IsBusy = true;
            try
            {
                var photoPath = await _photoPickerService.PickAndSavePhotoAsync();

                if (!string.IsNullOrEmpty(photoPath))
                {
                    // Store the previous path in case we need to revert
                    string previousPath = UserProfile.PhotoUrl;

                    // Update the path
                    UserProfile.PhotoUrl = photoPath;

                    // Verify the file is accessible
                    if (!_photoPickerService.IsPathValid(photoPath))
                    {
                        // Revert to the previous path if the new one is invalid
                        UserProfile.PhotoUrl = previousPath;

                        await Shell.Current.DisplayAlert("Avviso",
                            "Impossibile accedere all'immagine selezionata. Riprova con un'altra immagine.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Errore",
                    $"Impossibile selezionare la foto: {ex.Message}", "OK");
            }
            finally
            {
                IsBusy = false;
            }
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