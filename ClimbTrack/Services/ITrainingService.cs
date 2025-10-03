using ClimbTrack.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface ITrainingService
    {
        Task<string> SaveTrainingSessionAsync(TrainingSession session);
        Task<ObservableCollection<TrainingSession>> GetUserTrainingSessionsAsync(string userId);
        Task<TrainingSession> GetTrainingSessionAsync(string userId, string sessionId);
        Task<bool> DeleteTrainingSessionAsync(string userId, string sessionId);
        Task<IEnumerable<ClimbingRoute>> GetRoutesByPanelAsync(string panelId);
        Task<Dictionary<string, int>> GetPreviousRouteAttemptsAsync(string panelId, string userId = null);
    }
}
