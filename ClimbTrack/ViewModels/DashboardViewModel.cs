using ClimbTrack.Models;
using ClimbTrack.Services;
using Microcharts;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly IDashboardService _dashboardService;
        private readonly IErrorHandlingService _errorHandlingService;

        private string _totalSessions;
        private string _totalRoutesCompleted;
        private string _completionRate;
        private string _totalTrainingTime;
        private string _averageSessionTime;
        private Chart _trainingFrequencyChart;
        private Chart _difficultyDistributionChart;
        private Chart _trainingTimeByDayChart;
        private Chart _completionRateByDifficultyChart;
        private Chart _attemptsPerDifficultyChart;
        private ObservableCollection<TrainingSession> _recentSessions;
        private bool _hasData;
        private bool _isRefreshing;

        // Individual chart loading indicators
        private bool _isTrainingFrequencyChartLoading;
        private bool _isDifficultyDistributionChartLoading;
        private bool _isTrainingTimeByDayChartLoading;
        private bool _isCompletionRateChartLoading;
        private bool _isAttemptsPerDifficultyChartLoading;

        public string TotalSessions
        {
            get => _totalSessions;
            set => SetProperty(ref _totalSessions, value);
        }

        public string TotalRoutesCompleted
        {
            get => _totalRoutesCompleted;
            set => SetProperty(ref _totalRoutesCompleted, value);
        }

        public string CompletionRate
        {
            get => _completionRate;
            set => SetProperty(ref _completionRate, value);
        }

        public string TotalTrainingTime
        {
            get => _totalTrainingTime;
            set => SetProperty(ref _totalTrainingTime, value);
        }

        public string AverageSessionTime
        {
            get => _averageSessionTime;
            set => SetProperty(ref _averageSessionTime, value);
        }

        public Chart TrainingFrequencyChart
        {
            get => _trainingFrequencyChart;
            set => SetProperty(ref _trainingFrequencyChart, value);
        }

        public Chart DifficultyDistributionChart
        {
            get => _difficultyDistributionChart;
            set => SetProperty(ref _difficultyDistributionChart, value);
        }

        public Chart TrainingTimeByDayChart
        {
            get => _trainingTimeByDayChart;
            set => SetProperty(ref _trainingTimeByDayChart, value);
        }

        public Chart CompletionRateByDifficultyChart
        {
            get => _completionRateByDifficultyChart;
            set => SetProperty(ref _completionRateByDifficultyChart, value);
        }

        public Chart AttemptsPerDifficultyChart
        {
            get => _attemptsPerDifficultyChart;
            set => SetProperty(ref _attemptsPerDifficultyChart, value);
        }

        public ObservableCollection<TrainingSession> RecentSessions
        {
            get => _recentSessions;
            set => SetProperty(ref _recentSessions, value);
        }

        public bool HasData
        {
            get => _hasData;
            set => SetProperty(ref _hasData, value);
        }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        // Individual chart loading properties
        public bool IsTrainingFrequencyChartLoading
        {
            get => _isTrainingFrequencyChartLoading;
            set => SetProperty(ref _isTrainingFrequencyChartLoading, value);
        }

        public bool IsDifficultyDistributionChartLoading
        {
            get => _isDifficultyDistributionChartLoading;
            set => SetProperty(ref _isDifficultyDistributionChartLoading, value);
        }

        public bool IsTrainingTimeByDayChartLoading
        {
            get => _isTrainingTimeByDayChartLoading;
            set => SetProperty(ref _isTrainingTimeByDayChartLoading, value);
        }

        public bool IsCompletionRateChartLoading
        {
            get => _isCompletionRateChartLoading;
            set => SetProperty(ref _isCompletionRateChartLoading, value);
        }

        public bool IsAttemptsPerDifficultyChartLoading
        {
            get => _isAttemptsPerDifficultyChartLoading;
            set => SetProperty(ref _isAttemptsPerDifficultyChartLoading, value);
        }

        public ICommand LoadDataCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ViewSessionDetailsCommand { get; }
        public ICommand GoToTrainingCommand { get; }

        public DashboardViewModel(IDashboardService dashboardService, IErrorHandlingService errorHandlingService)
        {
            Title = "Dashboard";
            _dashboardService = dashboardService;
            _errorHandlingService = errorHandlingService;
            _recentSessions = new ObservableCollection<TrainingSession>();

            // Initialize commands
            LoadDataCommand = new Command(async () => await LoadDataAsync());
            RefreshCommand = new Command(async () => await RefreshAsync());
            ViewSessionDetailsCommand = new Command<TrainingSession>(async (session) => await ViewSessionDetailsAsync(session));
            GoToTrainingCommand = new Command(async () => await GoToTrainingAsync());
        }

        private async Task LoadDataAsync()
        {
            await ExecuteWithErrorHandlingAsync(async () =>
            {
                try
                {
                    // Load summary data first (this is quick)
                    var summary = await _dashboardService.GetDashboardSummaryAsync();
                    TotalSessions = summary.TotalSessions.ToString();
                    TotalRoutesCompleted = summary.TotalRoutesCompleted.ToString();
                    CompletionRate = $"{summary.CompletionRate:F1}%";
                    TotalTrainingTime = summary.FormattedTotalTime;
                    AverageSessionTime = summary.FormattedAvgTime;

                    // Set HasData flag early so UI can show
                    HasData = summary.TotalSessions > 0;

                    // If there's no data, don't try to load charts
                    if (!HasData)
                        return;

                    // Load recent sessions (usually small data)
                    var recentSessions = await _dashboardService.GetRecentSessionsAsync();
                    RecentSessions.Clear();
                    foreach (var session in recentSessions)
                    {
                        RecentSessions.Add(session);
                    }

                    // Now load charts one by one
                    await LoadChartsSequentiallyAsync();
                }
                catch (Exception ex)
                {
                    // Handle specific exceptions
                    if (ex is UnauthorizedAccessException)
                    {
                        await _errorHandlingService.HandleAuthenticationExceptionAsync(ex, "LoadDashboardData");
                    }
                    else
                    {
                        await _errorHandlingService.HandleExceptionAsync(ex, "LoadDashboardData", true);
                    }

                    // Set default values or clear data
                    HasData = false;
                    RecentSessions.Clear();
                    TrainingFrequencyChart = null;
                    DifficultyDistributionChart = null;
                    TrainingTimeByDayChart = null;
                    CompletionRateByDifficultyChart = null;
                    AttemptsPerDifficultyChart = null;
                }
            }, "LoadDashboardData");
        }

        private async Task LoadChartsSequentiallyAsync()
        {
            // Load Training Frequency Chart
            await LoadTrainingFrequencyChartAsync();

            // Load Difficulty Distribution Chart
            await LoadDifficultyDistributionChartAsync();

            // Load Training Time By Day Chart
            await LoadTrainingTimeByDayChartAsync();

            // Load Completion Rate Chart
            await LoadCompletionRateChartAsync();

            // Load Attempts Per Difficulty Chart
            await LoadAttemptsPerDifficultyChartAsync();
        }

        private async Task LoadTrainingFrequencyChartAsync()
        {
            IsTrainingFrequencyChartLoading = true;
            try
            {
                var frequencyData = await _dashboardService.GetTrainingFrequencyDataAsync();
                TrainingFrequencyChart = CreateTrainingFrequencyChart(frequencyData);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "LoadTrainingFrequencyChart", false);
                TrainingFrequencyChart = null;
            }
            finally
            {
                IsTrainingFrequencyChartLoading = false;
            }
        }

        private async Task LoadDifficultyDistributionChartAsync()
        {
            IsDifficultyDistributionChartLoading = true;
            try
            {
                var difficultyData = await _dashboardService.GetRouteDifficultyDistributionAsync();
                DifficultyDistributionChart = CreateDifficultyDistributionChart(difficultyData);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "LoadDifficultyDistributionChart", false);
                DifficultyDistributionChart = null;
            }
            finally
            {
                IsDifficultyDistributionChartLoading = false;
            }
        }

        private async Task LoadTrainingTimeByDayChartAsync()
        {
            IsTrainingTimeByDayChartLoading = true;
            try
            {
                var timeByDayData = await _dashboardService.GetTrainingTimeByDayAsync();
                TrainingTimeByDayChart = CreateTrainingTimeByDayChart(timeByDayData);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "LoadTrainingTimeByDayChart", false);
                TrainingTimeByDayChart = null;
            }
            finally
            {
                IsTrainingTimeByDayChartLoading = false;
            }
        }

        private async Task LoadCompletionRateChartAsync()
        {
            IsCompletionRateChartLoading = true;
            try
            {
                var completionRateData = await _dashboardService.GetCompletionRateByDifficultyAsync();
                CompletionRateByDifficultyChart = CreateCompletionRateChart(completionRateData);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "LoadCompletionRateChart", false);
                CompletionRateByDifficultyChart = null;
            }
            finally
            {
                IsCompletionRateChartLoading = false;
            }
        }

        private async Task LoadAttemptsPerDifficultyChartAsync()
        {
            IsAttemptsPerDifficultyChartLoading = true;
            try
            {
                var attemptsData = await _dashboardService.GetAverageAttemptsPerDifficultyAsync();
                AttemptsPerDifficultyChart = CreateAttemptsPerDifficultyChart(attemptsData);
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, "LoadAttemptsPerDifficultyChart", false);
                AttemptsPerDifficultyChart = null;
            }
            finally
            {
                IsAttemptsPerDifficultyChartLoading = false;
            }
        }

        private async Task RefreshAsync()
        {
            IsRefreshing = true;
            await LoadDataAsync();
            IsRefreshing = false;
        }

        private async Task ViewSessionDetailsAsync(TrainingSession session)
        {
            if (session == null)
                return;

            // Navigate to session details page
            var parameters = new Dictionary<string, object>
            {
                { "SessionId", session.Id }
            };

            await Shell.Current.GoToAsync("SessionDetailsPage", parameters);
        }

        private async Task GoToTrainingAsync()
        {
            await Shell.Current.GoToAsync("//HomePage");
        }

        // Chart creation methods
        private Chart CreateTrainingFrequencyChart(List<DateValuePair> data)
        {
            if (data == null || !data.Any())
                return null;

            var entries = new List<ChartEntry>();

            // Only show last 14 days for better visibility
            var recentData = data.Skip(Math.Max(0, data.Count - 14)).ToList();

            foreach (var item in recentData)
            {
                entries.Add(new ChartEntry(item.Value)
                {
                    Label = item.Label,
                    ValueLabel = item.Value.ToString(),
                    Color = SKColor.Parse("#3498db")
                });
            }

            return new BarChart
            {
                Entries = entries,
                LabelTextSize = 30f,
                ValueLabelOrientation = Orientation.Horizontal,
                LabelOrientation = Orientation.Horizontal,
                BackgroundColor = SKColors.Transparent
            };
        }

        private Chart CreateDifficultyDistributionChart(Dictionary<string, int> data)
        {
            if (data == null || !data.Any())
                return null;

            var entries = new List<ChartEntry>();
            var colors = new[] { "#f39c12", "#e74c3c", "#9b59b6", "#3498db", "#2ecc71", "#1abc9c", "#34495e" };
            int colorIndex = 0;

            foreach (var item in data.OrderBy(x => int.Parse(x.Key)))
            {
                entries.Add(new ChartEntry(item.Value)
                {
                    Label = item.Key,
                    ValueLabel = item.Value.ToString(),
                    Color = SKColor.Parse(colors[colorIndex % colors.Length])
                });
                colorIndex++;
            }

            return new DonutChart
            {
                Entries = entries,
                LabelTextSize = 30f,
                BackgroundColor = SKColors.Transparent
            };
        }

        private Chart CreateTrainingTimeByDayChart(Dictionary<string, double> data)
        {
            if (data == null || !data.Any())
                return null;

            var entries = new List<ChartEntry>();
            var dayOrder = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

            foreach (var day in dayOrder)
            {
                if (data.TryGetValue(day, out double value))
                {
                    entries.Add(new ChartEntry((float)value)
                    {
                        Label = day,
                        ValueLabel = $"{value:F0} min",
                        Color = SKColor.Parse("#3498db")
                    });
                }
            }

            return new BarChart
            {
                Entries = entries,
                LabelTextSize = 30f,
                ValueLabelOrientation = Orientation.Horizontal,
                LabelOrientation = Orientation.Horizontal,
                BackgroundColor = SKColors.Transparent
            };
        }


        private Chart CreateCompletionRateChart(Dictionary<string, int> data)
        {
            if (data == null || !data.Any())
                return null;

            var entries = new List<ChartEntry>();

            foreach (var item in data.OrderBy(x => int.Parse(x.Key)))
            {
                // Use a color gradient based on completion rate (red to green)
                var hue = (item.Value / 100.0) * 120; // 0 = red, 120 = green
                var color = SKColor.FromHsl((float)hue, 0.75f, 0.5f);

                entries.Add(new ChartEntry(item.Value)
                {
                    Label = item.Key,
                    ValueLabel = $"{item.Value}%",
                    Color = color
                });
            }

            return new PointChart
            {
                Entries = entries,
                LabelTextSize = 30f,
                PointSize = 18f,
                PointMode = PointMode.Circle,
                BackgroundColor = SKColors.Transparent
            };
        }

        private Chart CreateAttemptsPerDifficultyChart(Dictionary<string, double> data)
        {
            if (data == null || !data.Any())
                return null;

            var entries = new List<ChartEntry>();

            foreach (var item in data.OrderBy(x => int.Parse(x.Key)))
            {
                entries.Add(new ChartEntry((float)item.Value)
                {
                    Label = item.Key,
                    ValueLabel = $"{item.Value:F1}",
                    Color = SKColor.Parse("#e74c3c")
                });
            }

            return new LineChart
            {
                Entries = entries,
                LabelTextSize = 30f,
                LineMode = LineMode.Straight,
                PointMode = PointMode.Circle,
                BackgroundColor = SKColors.Transparent
            };
        }

        public async Task OnAppearingAsync()
        {
            await LoadDataAsync();
        }
    }
}