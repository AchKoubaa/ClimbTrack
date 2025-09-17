using ClimbTrack.Models;
using ClimbTrack.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    [QueryProperty(nameof(Panel), "Panel")]
    [QueryProperty(nameof(RoutesParameter), "Routes")]
    [QueryProperty(nameof(UserId), "UserId")]

    public class TrainingViewModel : BaseViewModel
    {
        private readonly ITrainingService _trainingService;
        private readonly INavigationService _navigationService;
        private readonly IAuthService _authService;

        private string _panel;
        private string _userId;
        private string _routesQuery;
        private ObservableCollection<ClimbingRoute> _routes;
        private ObservableCollection<TrainingRouteItem> _trainingRoutes;
        private DateTime _startTime;
        private TimeSpan _elapsedTime;
        private bool _isTrainingActive;
        private System.Timers.Timer _timer;

        public string Panel
        {
            get => _panel;
            set
            {
                SetProperty(ref _panel, value);
                if (!string.IsNullOrEmpty(value))
                {
                    LoadRoutesByPanel(value);
                }
            }
        }
        public string UserId
        {
            get => _userId;
            set => SetProperty(ref _userId, value);
        }


        private async void LoadRoutesByPanel(string panelId)
        {
            try
            {
                IsBusy = true;

                var routes = await _trainingService.GetRoutesByPanelAsync(panelId);
                Routes = new ObservableCollection<ClimbingRoute>(routes);

                // Get previous attempts for routes
                var previousAttempts = await _trainingService.GetPreviousRouteAttemptsAsync(panelId, UserId);

                InitializeTrainingRoutes(previousAttempts);
            }
            catch (Exception ex)
            {
                // Handle error
                Debug.WriteLine($"Error loading routes: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }


        public ObservableCollection<ClimbingRoute> Routes
        {
            get => _routes;
            set => SetProperty(ref _routes, value);
        }

        public object RoutesParameter
        {
            set
            {
                if (value is ObservableCollection<ClimbingRoute> routes)
                {
                    Routes = routes;
                    // Load previous attempts asynchronously
                    LoadPreviousAttemptsForRoutes();
                }
            }
        }

        private async void LoadPreviousAttemptsForRoutes()
        {
            try
            {
                if (Routes != null && !string.IsNullOrEmpty(Panel))
                {
                    // Get user ID
                    string userId = string.IsNullOrEmpty(UserId) ? await _authService.GetUserId() : UserId;

                    // Get previous attempts for routes
                    var previousAttempts = await _trainingService.GetPreviousRouteAttemptsAsync(Panel, userId);

                    // Initialize training routes with previous attempts data
                    InitializeTrainingRoutes(previousAttempts);
                }
                else
                {
                    // If no panel is set or no routes, initialize with empty attempts
                    InitializeTrainingRoutes();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading previous attempts: {ex.Message}");
                // Initialize with empty attempts if there's an error
                InitializeTrainingRoutes();
            }
        }

        public ObservableCollection<TrainingRouteItem> TrainingRoutes
        {
            get => _trainingRoutes;
            set => SetProperty(ref _trainingRoutes, value);
        }

        public TimeSpan ElapsedTime
        {
            get => _elapsedTime;
            set => SetProperty(ref _elapsedTime, value);
        }

        public bool IsTrainingActive
        {
            get => _isTrainingActive;
            set => SetProperty(ref _isTrainingActive, value);
        }

        public string ElapsedTimeDisplay => $"{ElapsedTime.Hours:00}:{ElapsedTime.Minutes:00}:{ElapsedTime.Seconds:00}";

        public ICommand StartTrainingCommand { get; }
        public ICommand EndTrainingCommand { get; }
        public ICommand ToggleRouteCompletionCommand { get; }
        public ICommand IncrementAttemptsCommand { get; }
        public ICommand DecrementAttemptsCommand { get; }
        public ICommand SelectRouteCommand { get; }

        public TrainingViewModel(
            ITrainingService trainingService,
            INavigationService navigationService,
            IAuthService authService)
        {
            _trainingService = trainingService;
            _navigationService = navigationService;
            _authService = authService;

            Title = "Allenamento";
            TrainingRoutes = new ObservableCollection<TrainingRouteItem>();
            ElapsedTime = TimeSpan.Zero;
            IsTrainingActive = false;

            //StartTrainingCommand = new Command(StartTraining);
            EndTrainingCommand = new Command(async () => await EndTraining());
            ToggleRouteCompletionCommand = new Command<TrainingRouteItem>(ToggleRouteCompletion);
            IncrementAttemptsCommand = new Command<TrainingRouteItem>(IncrementAttempts);
            DecrementAttemptsCommand = new Command<TrainingRouteItem>(DecrementAttempts);
            SelectRouteCommand = new Command<TrainingRouteItem>(SelectRoute);
            _timer = new System.Timers.Timer(1000);
            _timer.Elapsed += (s, e) =>
            {
                ElapsedTime = DateTime.Now - _startTime;
                OnPropertyChanged(nameof(ElapsedTimeDisplay));
            };
        }

        private void InitializeTrainingRoutes(Dictionary<string, int> previousAttempts = null)
        {
            TrainingRoutes.Clear();
            if (Routes != null)
            {
                foreach (var route in Routes)
                {
                    // Check if we have previous attempts data for this route
                    int attempts = 0;
                    bool wasPreviouslyCompleted = false;

                    if (previousAttempts != null && previousAttempts.ContainsKey(route.Id))
                    {
                        attempts = previousAttempts[route.Id];
                        wasPreviouslyCompleted = attempts > 0; // If there were attempts, consider it completed
                    }

                    TrainingRoutes.Add(new TrainingRouteItem
                    {
                        Route = route,
                        IsCompleted = false, // Start uncompleted for new session
                        Attempts = attempts, // Use total previous attempts count
                        PreviousAttempts = attempts, // Store previous attempts separately
                        WasPreviouslyCompleted = wasPreviouslyCompleted
                    });
                }
            }
        }

        //private void StartTraining()
        //{
        //    if (!IsTrainingActive)
        //    {
        //        _startTime = DateTime.Now;
        //        ElapsedTime = TimeSpan.Zero;
        //        // Reset all routes to unselected
        //        foreach (var route in TrainingRoutes)
        //        {
        //            route.IsSelected = false;
        //        }
        //        IsTrainingActive = true;
        //        _timer.Start();
        //    }
        //}

        private async Task EndTraining()
        {
            if (IsTrainingActive)
            {
                _timer.Stop();
                IsTrainingActive = false;

                // Check if any route is selected
                var selectedRoute = TrainingRoutes.FirstOrDefault(r => r.IsSelected);

                if (selectedRoute != null)
                {
                    bool shouldSave = await Application.Current.MainPage.DisplayAlert(
                        "Fine Allenamento",
                        "Vuoi salvare questa sessione di allenamento?",
                        "Sì", "No");

                    if (shouldSave)
                    {
                        await SaveTrainingSession();
                    }
                }
                else
                {
                    // No route selected, just show a message
                    await Application.Current.MainPage.DisplayAlert(
                        "Fine Allenamento",
                        "Nessun percorso selezionato. La sessione non verrà salvata.",
                        "OK");
                }

                // Reset all routes to unselected
                foreach (var route in TrainingRoutes)
                {
                    route.IsSelected = false;
                }

                await _navigationService.GoBackAsync();
            }
        }

        private async Task SaveTrainingSession()
        {
            try
            {
                // Verifica se l'utente è autenticato o se è stato passato un ID utente
                string userId = UserId;

                // Se non è stato passato un ID utente, ottienilo dal servizio di autenticazione
                if (string.IsNullOrEmpty(userId))
                {
                    userId = await _authService.GetUserId();

                    if (string.IsNullOrEmpty(userId))
                    {
                        await Application.Current.MainPage.DisplayAlert(
                            "Errore",
                            "Devi essere autenticato per salvare la sessione di allenamento.",
                            "OK");
                        return;
                    }
                }

                // Find the selected route item
                var selectedRoute = TrainingRoutes.FirstOrDefault(r => r.IsSelected);

                // If no route is selected, show an error and return
                if (selectedRoute == null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Errore",
                        "Nessun percorso selezionato. Seleziona un percorso prima di terminare l'allenamento.",
                        "OK");
                    return;
                }

                var session = new TrainingSession
                {
                    UserId = userId,
                    PanelType = Panel,
                    Duration = ElapsedTime,
                    Timestamp = DateTime.Now,
                    CompletedRoutes = new List<CompletedRoute>
            {
                new CompletedRoute
                {
                    RouteId = selectedRoute.Route.Id,
                    Completed = selectedRoute.IsCompleted,
                    Attempts = selectedRoute.Attempts - selectedRoute.PreviousAttempts
                }
            }
                };

                // Usa il servizio specializzato per salvare la sessione
                await _trainingService.SaveTrainingSessionAsync(session);
                await Application.Current.MainPage.DisplayAlert("Successo", "Sessione di allenamento salvata con successo!", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Errore", $"Impossibile salvare la sessione: {ex.Message}", "OK");
            }
        }

        private void ToggleRouteCompletion(TrainingRouteItem item)
        {
            if (item != null)
            {
                item.IsCompleted = !item.IsCompleted;
                // Se è la prima volta che viene completato, incrementa i tentativi
                //if (item.IsCompleted && item.Attempts == 0)
                //{
                //    item.Attempts = 1;
                //}
            }
        }

        private void IncrementAttempts(TrainingRouteItem item)
        {
            if (item != null)
            {
                item.Attempts++;
            }
        }

        private void DecrementAttempts(TrainingRouteItem item)
        {
            if (item != null && item.Attempts > 0)
            {
                item.Attempts--;
            }
        }
        private void SelectRoute(TrainingRouteItem item)
        {
            if (item != null)
            {
                // If training is not active, start it
                if (!IsTrainingActive)
                {
                    _startTime = DateTime.Now;
                    ElapsedTime = TimeSpan.Zero;
                    IsTrainingActive = true;
                    _timer.Start();
                }

                // Deselect all other items
                foreach (var route in TrainingRoutes)
                {
                    if (route != item)
                    {
                        route.IsSelected = false;
                    }
                }

                // Toggle selection for the clicked item
                item.IsSelected = !item.IsSelected;
            }
        }
    }
    public class TrainingRouteItem : BaseModel
    {
        private ClimbingRoute _route;
        private bool _isCompleted;
        private int _attempts;
        private int _previousAttempts;
        private bool _wasPreviouslyCompleted;
        private bool _isSelected;
        public ClimbingRoute Route
        {
            get => _route;
            set => SetProperty(ref _route, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            set
            {
                bool oldValue = _isCompleted;
                if (SetProperty(ref _isCompleted, value))
                {
                    // If changing from uncompleted to completed, increment attempts
                    if (value && !oldValue)
                    {
                        Attempts++;
                        OnPropertyChanged(nameof(AttemptsDisplay)); // Explicitly notify
                    }
                    // If changing from completed to uncompleted, decrement attempts
                    // but don't go below the previous attempts count
                    else if (!value && oldValue && _attempts > _previousAttempts)
                    {
                        Attempts--;
                        OnPropertyChanged(nameof(AttemptsDisplay)); // Explicitly notify
                    }
                }
            }
        }
        public int Attempts
        {
            get => _attempts;
            set
            {
                if (SetProperty(ref _attempts, value))
                {
                    OnPropertyChanged(nameof(AttemptsDisplay));
                }
            }
        }

        public int PreviousAttempts
        {
            get => _previousAttempts;
            set => SetProperty(ref _previousAttempts, value);
        }

        public bool WasPreviouslyCompleted
        {
            get => _wasPreviouslyCompleted;
            set => SetProperty(ref _wasPreviouslyCompleted, value);
        }

        // Add a property to show total attempts (current + previous)
        [JsonIgnore]
        public string AttemptsDisplay => _attempts.ToString();
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}