using ClimbTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface IDashboardService
    {
        Task<DashboardSummary> GetDashboardSummaryAsync();
        Task<List<DateValuePair>> GetTrainingFrequencyDataAsync(int days = 30);
        Task<Dictionary<string, int>> GetRouteDifficultyDistributionAsync();
        Task<Dictionary<string, double>> GetTrainingTimeByDayAsync();
        Task<List<TrainingSession>> GetRecentSessionsAsync(int count = 5);
        Task<Dictionary<string, int>> GetCompletionRateByDifficultyAsync();
        Task<Dictionary<string, double>> GetAverageAttemptsPerDifficultyAsync();
    }

    public class DashboardSummary
    {
        public int TotalSessions { get; set; }
        public int TotalRoutesCompleted { get; set; }
        public double CompletionRate { get; set; }
        public TimeSpan TotalTrainingTime { get; set; }
        public string FormattedTotalTime => $"{TotalTrainingTime.Hours}h {TotalTrainingTime.Minutes}m";
        public double AverageSessionTime { get; set; }
        public string FormattedAvgTime => $"{(int)AverageSessionTime}m";
    }

    public class DateValuePair
    {
        public DateTime Date { get; set; }
        public int Value { get; set; }
        public string Label => Date.ToString("dd/MM");
    }
}
