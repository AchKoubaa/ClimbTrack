using ClimbTrack.Models;
using ClimbTrack.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class HistoricalViewModel : BaseViewModel
    {
        private readonly ITrainingService _trainingService;
        private readonly INavigationService _navigationService;
        private readonly IAuthService _authService;

        private ObservableCollection<TrainingSession> _sessions;
        private bool _isRefreshing;
        private bool _isLoading;
        private string _filterPanel;

        public ObservableCollection<TrainingSession> Sessions
        {
            get => _sessions;
            set => SetProperty(ref _sessions, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string FilterPanel
        {
            get => _filterPanel;
            set
            {
                if (SetProperty(ref _filterPanel, value))
                {
                    ApplyFilters();
                }
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        public ICommand ViewDetailsCommand { get; }
        public ICommand DeleteSessionCommand { get; }
        public ICommand ShareSessionCommand { get; }

        public HistoricalViewModel(
            ITrainingService trainingService,
            INavigationService navigationService,
            IAuthService authService)
        {
            _trainingService = trainingService;
            _navigationService = navigationService;
            _authService = authService;

            Title = "Storico Allenamenti";
            Sessions = new ObservableCollection<TrainingSession>();

            RefreshCommand = new Command(async () => await Refresh());
            ClearFiltersCommand = new Command(ClearFilters);
            ViewDetailsCommand = new Command<TrainingSession>(async (session) => await ViewSessionDetails(session));
            DeleteSessionCommand = new Command<TrainingSession>(async (session) => await DeleteSession(session));
            ShareSessionCommand = new Command<TrainingSession>(async (session) => await ShareSession(session));
        }

        public async Task Initialize()
        {
            await LoadSessions();
        }

        private async Task LoadSessions()
        {
            try
            {
                IsLoading = true;

                // Check if user is authenticated
                if (!await _authService.IsAuthenticated())
                {
                    Debug.WriteLine("User not authenticated, redirecting to login page");
                    await Shell.Current.GoToAsync("login");
                    return;
                }
                // Get user ID
                string userId = await _authService.GetUserId();

                // Get all training sessions
                var allSessions = await _trainingService.GetUserTrainingSessionsAsync(userId);

                // Store all sessions for filtering
                var orderedSessions = allSessions.OrderByDescending(s => s.Timestamp).ToList();

                Sessions.Clear();
                foreach (var session in orderedSessions)
                {
                    Sessions.Add(session);
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Errore",
                    $"Impossibile caricare le sessioni di allenamento: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task Refresh()
        {
            IsRefreshing = true;
            await LoadSessions();
            IsRefreshing = false;
        }

        private void ApplyFilters()
        {
            try
            {
                IsLoading = true;

                // Get all sessions
                var allSessions = Sessions.ToList();

                // Apply panel filter
                if (!string.IsNullOrEmpty(FilterPanel))
                {
                    allSessions = allSessions.Where(s => s.PanelType.Contains(FilterPanel, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Update the collection
                Sessions.Clear();
                foreach (var session in allSessions.OrderByDescending(s => s.Timestamp))
                {
                    Sessions.Add(session);
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ClearFilters()
        {
            FilterPanel = null;

            // Reload all sessions
            LoadSessions();
        }

        private async Task ViewSessionDetails(TrainingSession session)
        {
            if (session == null) return;

            // Navigate to details page
            await _navigationService.NavigateToAsync("sessionDetails", new Dictionary<string, object>
            {
                  { "id", session.Id },
                  { "userId", session.UserId }
            });
        }

        private async Task DeleteSession(TrainingSession session)
        {
            if (session == null) return;

            bool confirm = await Shell.Current.DisplayAlert(
                "Conferma eliminazione",
                "Sei sicuro di voler eliminare questa sessione di allenamento?",
                "Sì", "No");

            if (confirm)
            {
                try
                {
                    // Verifica se l'utente è autenticato
                    var currentUser = _authService.GetCurrentUser();
                    if (currentUser == null && string.IsNullOrEmpty(session.UserId))
                    {
                        await Shell.Current.DisplayAlert(
                            "Errore",
                            "Non è possibile eliminare la sessione: utente non autenticato.",
                            "OK");
                        return;
                    }

                    // Usa l'ID utente della sessione o quello dell'utente corrente
                    string userId = !string.IsNullOrEmpty(session.UserId) ? session.UserId : currentUser.Uid;

                    // Elimina la sessione utilizzando il servizio specializzato
                    bool success = await _trainingService.DeleteTrainingSessionAsync(userId, session.Id);

                    if (success)
                    {
                        // Remove from the collection
                        Sessions.Remove(session);
                        await Shell.Current.DisplayAlert("Successo", "Sessione eliminata con successo!", "OK");
                        await Shell.Current.GoToAsync("..");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Errore", "Impossibile eliminare la sessione.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert(
                        "Errore",
                        $"Impossibile eliminare la sessione: {ex.Message}",
                        "OK");
                }
            }
        }

        private async Task ShareSession(TrainingSession session)
        {
            if (session == null) return;

            try
            {
                // Create share text
                string shareText = $"Ho completato un allenamento il {session.FormattedDate}!\n" +
                                  $"Durata: {session.FormattedDuration}\n" +
                                  $"Pannello: {session.PanelType}\n" +
                                  $"Percorsi completati: {session.CompletedRoutesCount}/{session.TotalRoutes}";

                // Use Share API
                await Share.RequestAsync(new ShareTextRequest
                {
                    Text = shareText,
                    Title = "Condividi sessione di allenamento"
                });
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert(
                    "Errore",
                    $"Impossibile condividere la sessione: {ex.Message}",
                    "OK");
            }
        }
    }
}