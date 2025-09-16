using ClimbTrack.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ITrainingService _trainingService;
        private readonly IClimbingService _climbingService;
        private readonly IAuthService _authService;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(ITrainingService trainingService, IClimbingService climbingService, IAuthService authService, ILogger<DashboardService> logger)
        {
            _trainingService = trainingService;
            _climbingService = climbingService;
            _authService = authService;
            _logger = logger;
        }
        public async Task<DashboardSummary> GetDashboardSummaryAsync()
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new DashboardSummary();
                }

                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);
                if (sessions == null || !sessions.Any())
                {
                    return new DashboardSummary();
                }

                int totalSessions = sessions.Count;
                int totalRoutesAttempted = sessions.Sum(s => s.TotalRoutes);
                int totalRoutesCompleted = sessions.Sum(s => s.CompletedRoutesCount);
                double completionRate = totalRoutesAttempted > 0 ? (double)totalRoutesCompleted / totalRoutesAttempted * 100 : 0;

                TimeSpan totalTime = TimeSpan.FromTicks(sessions.Sum(s => s.Duration.Ticks));
                double avgSessionTime = sessions.Count > 0 ? totalTime.TotalMinutes / sessions.Count : 0;

                return new DashboardSummary
                {
                    TotalSessions = totalSessions,
                    TotalRoutesCompleted = totalRoutesCompleted,
                    CompletionRate = completionRate,
                    TotalTrainingTime = totalTime,
                    AverageSessionTime = avgSessionTime
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard summary");
                return new DashboardSummary();
            }
        }

        public async Task<List<DateValuePair>> GetTrainingFrequencyDataAsync(int days = 30)
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new List<DateValuePair>();
                }

                var startDate = DateTime.Now.AddDays(-days);
                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);

                if (sessions == null || !sessions.Any())
                {
                    return new List<DateValuePair>();
                }

                // Filter sessions within the date range
                var filteredSessions = sessions.Where(s => s.Timestamp >= startDate).ToList();

                // Group sessions by date
                var groupedByDate = filteredSessions
                    .GroupBy(s => s.Timestamp.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key, g => g.Count());

                var result = new List<DateValuePair>();

                // Fill in all dates in the range
                for (var date = startDate.Date; date <= DateTime.Now.Date; date = date.AddDays(1))
                {
                    result.Add(new DateValuePair
                    {
                        Date = date,
                        Value = groupedByDate.ContainsKey(date) ? groupedByDate[date] : 0
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training frequency data");
                return new List<DateValuePair>();
            }
        }

        public async Task<Dictionary<string, int>> GetRouteDifficultyDistributionAsync()
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new Dictionary<string, int>();
                }

                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);
                if (sessions == null || !sessions.Any())
                {
                    return new Dictionary<string, int>();
                }

                // We need to get all routes that were completed
                var difficultyDistribution = new Dictionary<string, int>();

                // For each session, get the panel type and then get routes for that panel
                foreach (var session in sessions)
                {
                    if (session.CompletedRoutes == null || !session.CompletedRoutes.Any())
                        continue;

                    var panelRoutes = await _climbingService.GetRoutesAsync(session.PanelType);
                    if (panelRoutes == null)
                        continue;

                    var routesDict = panelRoutes.ToDictionary(r => r.Id, r => r);

                    foreach (var completedRoute in session.CompletedRoutes.Where(r => r.Completed))
                    {
                        if (routesDict.TryGetValue(completedRoute.RouteId, out var route))
                        {
                            string difficultyKey = route.Difficulty.ToString();
                            if (difficultyDistribution.ContainsKey(difficultyKey))
                            {
                                difficultyDistribution[difficultyKey]++;
                            }
                            else
                            {
                                difficultyDistribution[difficultyKey] = 1;
                            }
                        }
                    }
                }

                return difficultyDistribution;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting route difficulty distribution");
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, double>> GetTrainingTimeByDayAsync()
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return GetEmptyDaysDictionary();
                }

                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);
                if (sessions == null || !sessions.Any())
                {
                    return GetEmptyDaysDictionary();
                }

                var result = GetEmptyDaysDictionary();

                foreach (var session in sessions)
                {
                    string dayName = session.Timestamp.DayOfWeek.ToString().Substring(0, 3);
                    result[dayName] += session.Duration.TotalMinutes;
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting training time by day");
                return GetEmptyDaysDictionary();
            }
        }

        public async Task<List<TrainingSession>> GetRecentSessionsAsync(int count = 5)
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new List<TrainingSession>();
                }

                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);
                if (sessions == null || !sessions.Any())
                {
                    return new List<TrainingSession>();
                }

                return sessions
                    .OrderByDescending(s => s.Timestamp)
                    .Take(count)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent sessions");
                return new List<TrainingSession>();
            }
        }

        public async Task<Dictionary<string, int>> GetCompletionRateByDifficultyAsync()
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new Dictionary<string, int>();
                }

                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);
                if (sessions == null || !sessions.Any())
                {
                    return new Dictionary<string, int>();
                }

                var attemptsByDifficulty = new Dictionary<string, int>();
                var completionsByDifficulty = new Dictionary<string, int>();

                // For each session, analyze routes
                foreach (var session in sessions)
                {
                    if (session.CompletedRoutes == null)
                        continue;

                    var panelRoutes = await _climbingService.GetRoutesAsync(session.PanelType);
                    if (panelRoutes == null)
                        continue;

                    var routesDict = panelRoutes.ToDictionary(r => r.Id, r => r);

                    foreach (var routeAttempt in session.CompletedRoutes)
                    {
                        if (routesDict.TryGetValue(routeAttempt.RouteId, out var route))
                        {
                            string difficultyKey = route.Difficulty.ToString();

                            // Count attempt
                            if (!attemptsByDifficulty.ContainsKey(difficultyKey))
                                attemptsByDifficulty[difficultyKey] = 0;
                            attemptsByDifficulty[difficultyKey]++;

                            // Count completion if route was completed
                            if (routeAttempt.Completed)
                            {
                                if (!completionsByDifficulty.ContainsKey(difficultyKey))
                                    completionsByDifficulty[difficultyKey] = 0;
                                completionsByDifficulty[difficultyKey]++;
                            }
                        }
                    }
                }

                // Calculate completion rates
                var completionRates = new Dictionary<string, int>();
                foreach (var difficulty in attemptsByDifficulty.Keys)
                {
                    int attempts = attemptsByDifficulty[difficulty];
                    int completions = completionsByDifficulty.ContainsKey(difficulty) ? completionsByDifficulty[difficulty] : 0;
                    int rate = attempts > 0 ? (completions * 100) / attempts : 0;
                    completionRates[difficulty] = rate;
                }

                return completionRates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting completion rate by difficulty");
                return new Dictionary<string, int>();
            }
        }

        public async Task<Dictionary<string, double>> GetAverageAttemptsPerDifficultyAsync()
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    return new Dictionary<string, double>();
                }

                var sessions = await _trainingService.GetUserTrainingSessionsAsync(userId);
                if (sessions == null || !sessions.Any())
                {
                    return new Dictionary<string, double>();
                }

                var attemptsByDifficulty = new Dictionary<string, List<int>>();

                // For each session, analyze routes
                foreach (var session in sessions)
                {
                    if (session.CompletedRoutes == null)
                        continue;

                    var panelRoutes = await _climbingService.GetRoutesAsync(session.PanelType);
                    if (panelRoutes == null)
                        continue;

                    var routesDict = panelRoutes.ToDictionary(r => r.Id, r => r);

                    foreach (var routeAttempt in session.CompletedRoutes.Where(r => r.Completed))
                    {
                        if (routesDict.TryGetValue(routeAttempt.RouteId, out var route))
                        {
                            string difficultyKey = route.Difficulty.ToString();

                            if (!attemptsByDifficulty.ContainsKey(difficultyKey))
                                attemptsByDifficulty[difficultyKey] = new List<int>();

                            attemptsByDifficulty[difficultyKey].Add(routeAttempt.Attempts);
                        }
                    }
                }

                // Calculate average attempts
                var avgAttempts = new Dictionary<string, double>();
                foreach (var difficulty in attemptsByDifficulty.Keys)
                {
                    var attempts = attemptsByDifficulty[difficulty];
                    if (attempts.Any())
                    {
                        avgAttempts[difficulty] = attempts.Average();
                    }
                    else
                    {
                        avgAttempts[difficulty] = 0;
                    }
                }

                return avgAttempts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting average attempts per difficulty");
                return new Dictionary<string, double>();
            }
        }

        private Dictionary<string, double> GetEmptyDaysDictionary()
        {
            return new Dictionary<string, double>
            {
                { "Mon", 0 },
                { "Tue", 0 },
                { "Wed", 0 },
                { "Thu", 0 },
                { "Fri", 0 },
                { "Sat", 0 },
                { "Sun", 0 }
            };
        }
    }
}
