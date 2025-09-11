using ClimbTrack.Models;
using ClimbTrack.Services;
using System;
using System.Collections.ObjectModel;
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
                await Application.Current.MainPage.DisplayAlert("Errore",
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
    }
}