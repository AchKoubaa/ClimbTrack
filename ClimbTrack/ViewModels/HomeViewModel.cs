using ClimbTrack.Models;
using ClimbTrack.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;
        private readonly IClimbingService _climbingService;

        private ObservableCollection<ClimbingRoute> _routes;
        private string _selectedPanel;
        private ObservableCollection<string> _availablePanels;
        private bool _isRefreshing;
        private bool _loginPromptShown = false;

        public ObservableCollection<ClimbingRoute> Routes
        {
            get => _routes;
            set => SetProperty(ref _routes, value);
        }

        public string SelectedPanel
        {
            get => _selectedPanel;
            set
            {
                if (SetProperty(ref _selectedPanel, value) && value != null && LoadRoutesCommand != null)
                {
                    LoadRoutesCommand.Execute(null);
                }
            }
        }

        public ObservableCollection<string> AvailablePanels
        {
            get => _availablePanels;
            set => SetProperty(ref _availablePanels, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public ICommand LoadRoutesCommand { get; private set; }
        public ICommand StartTrainingCommand { get; private set; }
        public ICommand RefreshCommand { get; private set; }

        public HomeViewModel(
            IAuthService authService,
            IClimbingService climbingService)
        {
            _authService = authService;
            _climbingService = climbingService;

            Title = "CLIMBING WORKOUTS";

            // Initialize collections
            Routes = new ObservableCollection<ClimbingRoute>();
            AvailablePanels = new ObservableCollection<string>();

            // Initialize commands
            LoadRoutesCommand = new Command(async () => await LoadRoutes());
            StartTrainingCommand = new Command(async () => await StartTraining());
            RefreshCommand = new Command(async () => await Refresh());

           
        }

        public async Task Initialize()
        {
            // Only check authentication once per session
            if (!_loginPromptShown)
            {
                var user = _authService.GetUserId();
                if (user == null)
                {
                    _loginPromptShown = true;
                    // Consider showing a toast or small notification instead of redirecting
                    // await Shell.Current.DisplayAlert("Accedi per salvare i tuoi progressi","ok", "cancel");
                }
            }

            // Load available panel types from the service
            try
            {
                var panelTypes = await _climbingService.GetPanelTypesAsync();
                if (panelTypes != null && panelTypes.Count > 0)
                {
                    AvailablePanels = panelTypes;

                    // Set the selected panel from the available panels
                    // If the current selected panel is not in the available panels, use the first one
                    if (string.IsNullOrEmpty(SelectedPanel) || !AvailablePanels.Contains(SelectedPanel))
                    {
                        SelectedPanel = AvailablePanels[0];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading panel types: {ex.Message}");
                // Fallback to default panel types already set in constructor
            }

            await LoadRoutes();
        }

        private async Task LoadRoutes()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    if (string.IsNullOrEmpty(SelectedPanel))
                    {
                        return;
                    }

                    // Utilizzo del servizio specializzato per ottenere i percorsi
                    var fetchedRoutes = await _climbingService.GetRoutesAsync(SelectedPanel);

                    Routes.Clear();
                    foreach (var route in fetchedRoutes)
                    {
                        // Assicurati che il colore esadecimale sia impostato
                        if (string.IsNullOrEmpty(route.ColorHex))
                        {
                            route.ColorHex = GetColorHexFromName(route.Color);
                        }
                        Routes.Add(route);
                    }
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Errore", $"Impossibile caricare i percorsi: {ex.Message}", "OK");
                }
            });
        }

        private async Task StartTraining()
        {
            await ExecuteWithBusy(async () =>
            {
                try
                {
                    // Check if user is authenticated
                    bool isAuthenticated = await _authService.IsAuthenticated();

                    // If not authenticated, prompt the user to login
                    if (!isAuthenticated)
                    {
                        bool shouldLogin = await Shell.Current.DisplayAlert(
                            "Login Richiesto",
                            "Devi essere loggato per tracciare i tuoi allenamenti. Vuoi accedere ora?",
                            "Sì", "No");

                        if (shouldLogin)
                        {
                            // Navigate to login page
                            await Shell.Current.GoToAsync("login");
                            return;
                        }
                        else
                        {
                            // User chose not to login, show a message
                            await Shell.Current.DisplayAlert(
                                "Modalità Limitata",
                                "Stai usando l'app in modalità limitata. Le tue attività non saranno salvate.",
                                "OK");
                        }
                    }

                    // Check if there are any routes available
                    if (Routes == null || Routes.Count == 0)
                    {
                        await Shell.Current.DisplayAlert(
                            "Nessun Percorso",
                            "Non ci sono percorsi disponibili per questo pannello.",
                            "OK");
                        return;
                    }

                    // Prepare parameters for navigation - ONLY include simple types
                    var parameters = new Dictionary<string, object>
                    {
                        { "Panel", SelectedPanel }
                    };

                    // Add user information if authenticated
                    if (isAuthenticated)
                    {
                        var currentUser = _authService.GetCurrentUser();
                        if (currentUser != null)
                        {
                            parameters.Add("UserId", currentUser.Uid);
                        }
                    }

                    // Navigate to training page
                    await Shell.Current.GoToAsync("//training", parameters);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error starting training: {ex.Message}");
                    await Shell.Current.DisplayAlert("Errore", "Si è verificato un errore durante l'avvio dell'allenamento.", "OK");
                }
            });
        }

        private async Task Refresh()
        {
            IsRefreshing = true;
            await LoadRoutes();
            IsRefreshing = false;
        }

        // Metodo helper per convertire i nomi dei colori in valori esadecimali
        private string GetColorHexFromName(string colorName)
        {
            return colorName?.ToLower() switch
            {
                "rosa" => "#FFC0CB",
                "bianco" => "#FFFFFF",
                "verde" => "#00FF00",
                "azzurro" => "#87CEEB",
                "blu" => "#0000FF",
                "grigio" => "#808080",
                "marrone" => "#A52A2A",
                "giallo" => "#FFFF00",
                "arancione" => "#FFA500",
                _ => "#CCCCCC" // Default color
            };
        }
    }
}