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
    [QueryProperty(nameof(SessionId), "id")]
    [QueryProperty(nameof(UserId), "userId")]
   
    public class SessionDetailsViewModel : BaseViewModel
    {
        private readonly ITrainingService _trainingService;
        private readonly IClimbingService _climbingService;
        private readonly INavigationService _navigationService;
        private readonly IAuthService _authService;

        private TrainingSession _session;
        private ObservableCollection<CompletedRouteViewModel> _completedRoutes;
        private string _panelTypeDisplay;
        private string _dateDisplay;
        private string _durationDisplay;
        private string _completionRateDisplay;

        public TrainingSession Session
        {
            get => _session;
            set => SetProperty(ref _session, value);
        }

        public object SessionParameter
        {
            set
            {
                if (value is TrainingSession session)
                {
                    Session = session;
                    UpdateSessionDisplayProperties();
                    LoadCompletedRoutes();
                }
            }
        }
        private string _sessionId;
        private string _userId;

        public string SessionId
        {
            get => _sessionId;
            set
            {
                _sessionId = value;
                LoadSessionById();
            }
        }

        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }

        public ObservableCollection<CompletedRouteViewModel> CompletedRoutes
        {
            get => _completedRoutes;
            set => SetProperty(ref _completedRoutes, value);
        }

        public string PanelTypeDisplay
        {
            get => _panelTypeDisplay;
            set => SetProperty(ref _panelTypeDisplay, value);
        }

        public string DateDisplay
        {
            get => _dateDisplay;
            set => SetProperty(ref _dateDisplay, value);
        }

        public string DurationDisplay
        {
            get => _durationDisplay;
            set => SetProperty(ref _durationDisplay, value);
        }

        public string CompletionRateDisplay
        {
            get => _completionRateDisplay;
            set => SetProperty(ref _completionRateDisplay, value);
        }

        public ICommand GoBackCommand { get; }
        public ICommand DeleteSessionCommand { get; }

        public SessionDetailsViewModel(
            ITrainingService trainingService,
            IClimbingService climbingService,
            INavigationService navigationService,
            IAuthService authService)
        {
            _trainingService = trainingService;
            _climbingService = climbingService;
            _navigationService = navigationService;
            _authService = authService;

            Title = "Dettagli Sessione";
            CompletedRoutes = new ObservableCollection<CompletedRouteViewModel>();

            GoBackCommand = new Command(async () => await _navigationService.NavigateToAsync("//historical"));
            DeleteSessionCommand = new Command(async () => await DeleteSession());
        }


        private void UpdateSessionDisplayProperties()
        {
            if (Session != null)
            {
                PanelTypeDisplay = $"Pannello: {Session.PanelType}";
                DateDisplay = Session.Timestamp.ToString("dd/MM/yyyy HH:mm");
                DurationDisplay = $"Durata: {Session.FormattedDuration}";

                int completedCount = Session.CompletedRoutesCount;
                int totalCount = Session.TotalRoutes;
                double percentage = totalCount > 0 ? (double)completedCount / totalCount * 100 : 0;

                CompletionRateDisplay = $"Completati: {completedCount}/{totalCount} ({percentage:F1}%)";
            }
        }

        private async void LoadSessionById()
        {
            // Check if the user is authenticated
            bool isAuthenticated = await _authService.IsAuthenticated();
            if (!isAuthenticated)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "Unable to load session: user not authenticated.",
                    "OK");
                await _navigationService.NavigateToAsync("//login");
                return;
            }

            // Get the user ID
            string userId = await _authService.GetUserId();
            if (string.IsNullOrEmpty(userId))
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "Unable to load session: user ID not found.",
                    "OK");
                return;
            }

            if (!string.IsNullOrEmpty(_sessionId))
            {
                try
                {
                    var session = await _trainingService.GetTrainingSessionAsync(userId, _sessionId);
                    if (session != null)
                    {
                        Session = session;
                        UpdateSessionDisplayProperties();
                        LoadCompletedRoutes();
                    }
                    else
                    {
                        // Handle the case when session is not found
                        await Application.Current.MainPage.DisplayAlert("Error",
                            "Session not found", "OK");
                        await _navigationService.GoBackAsync();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading session by ID: {ex.Message}");
                    await Application.Current.MainPage.DisplayAlert("Error",
                        "Unable to load session details", "OK");
                    await _navigationService.GoBackAsync();
                }
            }
        }

        private void LoadCompletedRoutes()
        {
            CompletedRoutes.Clear();

            if (Session?.CompletedRoutes != null)
            {
                // Carica i dettagli dei percorsi completati
                LoadRouteDetails();
            }
        }

        private async void LoadRouteDetails()
        {
            try
            {
                // Per ogni percorso completato, carica i dettagli dal database
                foreach (var route in Session.CompletedRoutes)
                {
                    // Ottieni i dettagli del percorso dal database utilizzando il servizio specializzato
                    var routeDetails = await _climbingService.GetRouteAsync(Session.PanelType, route.RouteId);

                    // Se il percorso non è stato trovato, crea un oggetto con informazioni minime
                    if (routeDetails == null)
                    {
                        routeDetails = new ClimbingRoute
                        {
                            Id = route.RouteId,
                            Name = $"Percorso {route.RouteId}",
                            Color = "Sconosciuto",
                            ColorHex = "#CCCCCC"
                        };
                    }

                    // Aggiungi il percorso alla lista
                    CompletedRoutes.Add(new CompletedRouteViewModel
                    {
                        Route = routeDetails,
                        Completed = route.Completed,
                        Attempts = route.Attempts
                    });
                }

                // Ordina i percorsi per difficoltà
                var sortedRoutes = new ObservableCollection<CompletedRouteViewModel>(
                    CompletedRoutes.OrderBy(r => r.Route?.Difficulty ?? 0));
                CompletedRoutes = sortedRoutes;
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Errore", $"Impossibile caricare i dettagli dei percorsi: {ex.Message}", "OK");
            }
        }

        private async Task DeleteSession()
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert(
                "Elimina Sessione",
                "Sei sicuro di voler eliminare questa sessione di allenamento? Questa azione non può essere annullata.",
                "Elimina", "Annulla");

            if (confirm)
            {
                try
                {
                    // Verifica se l'utente è autenticato
                    bool isAuthenticated = await _authService.IsAuthenticated();
                    if (!isAuthenticated)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                           "Errore",
                           "Non è possibile eliminare la sessione: utente non autenticato.",
                           "OK");
                        await _navigationService.NavigateToAsync("//login");
                        return;
                    }

                    // Verifica se l'utente è autenticato
                    string userId = await _authService.GetUserId();
                    if (userId == null && string.IsNullOrEmpty(Session.UserId))
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Errore",
                            "Non è possibile eliminare la sessione: utente non autenticato.",
                            "OK");
                        return;
                    }

                    if(SessionId == null)
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Errore",
                            "Non è possibile eliminare la sessione: sessione non caricata.",
                            "OK");
                        return;
                    }

                    // Elimina la sessione utilizzando il servizio specializzato
                    bool success = await _trainingService.DeleteTrainingSessionAsync(userId, SessionId);

                    if (success)
                    {
                        await Application.Current.MainPage.DisplayAlert("Successo", "Sessione eliminata con successo!", "OK");
                        await _navigationService.NavigateToAsync("//historical");
                    }
                    else
                    {
                        await Application.Current.MainPage.DisplayAlert("Errore", "Impossibile eliminare la sessione.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Errore", $"Impossibile eliminare la sessione: {ex.Message}", "OK");
                }
            }
        }
    }

    public class CompletedRouteViewModel : BaseModel
    {
        private ClimbingRoute _route;
        private bool _completed;
        private int _attempts;

        public ClimbingRoute Route
        {
            get => _route;
            set => SetProperty(ref _route, value);
        }

        public bool Completed
        {
            get => _completed;
            set => SetProperty(ref _completed, value);
        }

        public int Attempts
        {
            get => _attempts;
            set => SetProperty(ref _attempts, value);
        }
    }
}