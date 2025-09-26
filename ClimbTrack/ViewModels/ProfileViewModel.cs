using ClimbTrack.Models;
using ClimbTrack.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Maui.Storage;
using System.Diagnostics;

namespace ClimbTrack.ViewModels
{
    public class ProfileViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IDatabaseService _databaseService;
        private readonly ITrainingService _trainingService;

        private UserProfile _userProfile;
        private ObservableCollection<TrainingSession> _recentSessions;
        private bool _isRefreshing;
        private string _totalTrainingTime;
        private int _totalSessions;
        private int _totalRoutesCompleted;

        public UserProfile UserProfile
        {
            get => _userProfile;
            set => SetProperty(ref _userProfile, value);
        }

        public ObservableCollection<TrainingSession> RecentSessions
        {
            get => _recentSessions;
            set => SetProperty(ref _recentSessions, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public string TotalTrainingTime
        {
            get => _totalTrainingTime;
            set => SetProperty(ref _totalTrainingTime, value);
        }

        public int TotalSessions
        {
            get => _totalSessions;
            set => SetProperty(ref _totalSessions, value);
        }

        public int TotalRoutesCompleted
        {
            get => _totalRoutesCompleted;
            set => SetProperty(ref _totalRoutesCompleted, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand EditProfileCommand { get; }
        public ICommand ViewSessionDetailsCommand { get; }
        public ICommand GoToAdminCommand { get; }
        public ICommand ViewHistoryCommand { get; }
       

        public ProfileViewModel(
            IAuthService authService,
            IDatabaseService databaseService,
            ITrainingService trainingService)
        {
            _authService = authService;
            _databaseService = databaseService;
            _trainingService = trainingService;

            Title = "Profilo";
            RecentSessions = new ObservableCollection<TrainingSession>();
            UserProfile = new UserProfile();

            RefreshCommand = new Command(async () => await Refresh());
            LogoutCommand = new Command(async () => await Logout());
            EditProfileCommand = new Command(async () => await EditProfile());
            ViewSessionDetailsCommand = new Command<TrainingSession>(async (session) => await ViewSessionDetails(session));
            GoToAdminCommand = new Command(async () => await GoToAdmin());
            ViewHistoryCommand = new Command(async () => await ViewHistory());
        }

        private async Task ViewHistory()
        {
            await Shell.Current.GoToAsync("///historical");
        }

        private async Task GoToAdmin()
        {
            await Shell.Current.GoToAsync("admin");
        }

        public async Task Initialize()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    // Check if user is authenticated
                    if (!await _authService.IsAuthenticated())
                    {
                        Debug.WriteLine("User not authenticated, redirecting to login page");
                        await Shell.Current.GoToAsync("///login");
                        return;
                    }
                    // Load the user profile
                    await LoadUserProfile();

                    // Load all sessions for statistics
                    await LoadSessionStatistics();
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Errore", $"Impossibile caricare i dati del profilo: {ex.Message}", "OK");
                }
            });
        }

        private async Task LoadSessionStatistics()
        {
            try
            {
                // Check if user is authenticated
                if (!await _authService.IsAuthenticated())
                {
                    Debug.WriteLine("User not authenticated, redirecting to login page");
                    await Shell.Current.GoToAsync("///login");
                    return;
                }
                // Get user ID
                string userId = await _authService.GetUserId();

                // Get all training sessions
                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);

                // Calculate statistics
                TotalSessions = sessions.Count;

                TimeSpan totalTime = TimeSpan.Zero;
                foreach (var session in sessions)
                {
                    totalTime += session.Duration;
                }
                TotalTrainingTime = $"{(int)totalTime.TotalHours:00}:{totalTime.Minutes:00}";

                TotalRoutesCompleted = sessions.Sum(s => s.CompletedRoutesCount);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session statistics: {ex.Message}");
                throw;
            }
        }

        private async Task LoadUserProfile()
        {
            try
            {
                // Verifica se l'utente è autenticato
                bool isAuthenticated = await _authService.IsAuthenticated();
                if (!isAuthenticated)
                {
                    await Shell.Current.GoToAsync("//login");
                    return;
                }

                // Ottieni l'ID utente
                string userId = await _authService.GetUserId();
                string userEmail = await _authService.GetUserEmail();

                // Carica il profilo utente dal database
                var profile = await _databaseService.GetItem<UserProfile>($"users/{userId}", "profile");

                if (profile == null)
                {
                    // Se il profilo non esiste, creane uno nuovo
                    profile = new UserProfile
                    {
                        Id = userId,
                        DisplayName = userEmail?.Split('@')[0] ?? "Utente",
                        Email = userEmail,
                        CreatedAt = DateTime.Now,
                        LastLoginAt = DateTime.Now
                    };

                    // Salva il nuovo profilo
                    await _databaseService.UpdateItem($"users/{userId}", "profile", profile);
                }

                UserProfile = profile;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading user profile: {ex.Message}");
                throw;
            }
        }

        private async Task LoadRecentSessions()
        {
            try
            {
                // Ottieni l'ID utente
                string userId = await _authService.GetUserId();

                // Ottieni le sessioni di allenamento recenti utilizzando il servizio specializzato
                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);

                RecentSessions.Clear();
                foreach (var session in sessions.OrderByDescending(s => s.Timestamp).Take(10))
                {
                    RecentSessions.Add(session);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading recent sessions: {ex.Message}");
                throw;
            }
        }

        private void CalculateStatistics()
        {
            // Calcola il tempo totale di allenamento
            TimeSpan totalTime = TimeSpan.Zero;
            foreach (var session in RecentSessions)
            {
                totalTime += session.Duration;
            }

            TotalTrainingTime = $"{totalTime.Hours:00}:{totalTime.Minutes:00}:{totalTime.Seconds:00}";

            // Calcola il numero totale di sessioni
            TotalSessions = RecentSessions.Count;

            // Calcola il numero totale di percorsi completati
            TotalRoutesCompleted = RecentSessions.Sum(s => s.CompletedRoutesCount);
        }

        private async Task Refresh()
        {
            IsRefreshing = true;
            await Initialize();
            IsRefreshing = false;
        }

        private async Task Logout()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Logout",
                "Sei sicuro di voler effettuare il logout?",
                "Sì", "No");

            if (confirm)
            {
                try
                {
                   
                        // Fallback if AppShell is not accessible
                        _authService.Logout();
                    if (Shell.Current is AppShell appShell)
                    {
                        await Shell.Current.GoToAsync("//login");
                    }



                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Errore", $"Impossibile effettuare il logout: {ex.Message}", "OK");
                }
            }
        }

        private async Task EditProfile()
        {
            await Shell.Current.GoToAsync("editProfile", new Dictionary<string, object>
    {
        { "ProfileId", UserProfile.Id }
    });
        }

        private async Task ViewSessionDetails(TrainingSession session)
        {
            if (session != null)
            {
                try
                {
                    // Generate a unique key for this session
                    string sessionKey = Guid.NewGuid().ToString();

                    // Serialize the session to JSON
                    string sessionJson = System.Text.Json.JsonSerializer.Serialize(session);

                    // Store the JSON in Preferences
                    Preferences.Set(sessionKey, sessionJson);

                    // Navigate using the key
                    await Shell.Current.GoToAsync("//sessionDetails", new Dictionary<string, object>
            {
                { "SessionKey", sessionKey }
            });
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error",
                        $"Unable to view session details: {ex.Message}", "OK");
                }
            }
        }
    }
}