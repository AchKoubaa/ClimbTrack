using ClimbTrack.Models;
using Google.Cloud.Firestore.V1;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public class TrainingService : ITrainingService
    {
        private readonly IDatabaseService _databaseService;
        private readonly IAuthService _authService;

        public TrainingService(IDatabaseService databaseService, IAuthService authService)
        {
            _databaseService = databaseService;
            _authService = authService;
        }

        public async Task SaveTrainingSessionAsync(TrainingSession session)
        {
            try
            {
                // Verifica se l'utente è autenticato
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedAccessException("User must be authenticated to save training sessions");
                }

                // Imposta l'ID utente
                session.UserId = userId;

                // Salva la sessione nel database
                string nodePath = $"trainingSessions/{userId}";

                if (string.IsNullOrEmpty(session.Id))
                {
                    // Nuova sessione
                    session.Id = await _databaseService.AddItem(nodePath, session);
                }
                else
                {
                    // Aggiornamento sessione esistente
                    await _databaseService.UpdateItem(nodePath, session.Id, session);
                }
            }
            catch (Exception ex)
            {
                // Log dell'errore
                Console.WriteLine($"Error saving training session: {ex.Message}");
                throw;
            }
        }

        public async Task<ObservableCollection<TrainingSession>> GetUserTrainingSessionsAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Se non è specificato un userId, usa quello dell'utente corrente
                userId = await _authService.GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedAccessException("User must be authenticated to get training sessions");
                }
            }

            string nodePath = $"trainingSessions/{userId}";
            return await _databaseService.GetItems<TrainingSession>(nodePath);
        }

        public async Task<TrainingSession> GetTrainingSessionAsync(string userId, string sessionId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Se non è specificato un userId, usa quello dell'utente corrente
                userId = await _authService.GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedAccessException("User must be authenticated to get training sessions");
                }
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));
            }

            string nodePath = $"trainingSessions/{userId}";
            return await _databaseService.GetItem<TrainingSession>(nodePath, sessionId);
        }

        public async Task<bool> DeleteTrainingSessionAsync(string userId, string sessionId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                // Se non è specificato un userId, usa quello dell'utente corrente
                userId = await _authService.GetUserId();

                if (string.IsNullOrEmpty(userId))
                {
                    throw new UnauthorizedAccessException("User must be authenticated to delete training sessions");
                }
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentException("Session ID cannot be empty", nameof(sessionId));
            }

            string nodePath = $"trainingSessions/{userId}";
            return await _databaseService.DeleteItem(nodePath, sessionId);
        }

        public async Task<IEnumerable<ClimbingRoute>> GetRoutesByPanelAsync(string panelId)
        {
            try
            {
                // Use your existing database service methods
                string nodePath = ($"routes/{panelId}"); // Adjust this path based on your database structure

                // Get all routes
                var allRoutes = await _databaseService.GetItems<ClimbingRoute>(nodePath);


                // Filter by PanelType
                var result = allRoutes.Where(route => route.PanelType == panelId)
                                        .OrderBy(route => route.Difficulty)
                                        .ToList();
                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error fetching routes for panel {panelId}: {ex.Message}");
                return new List<ClimbingRoute>();
            }
        }

        public async Task<Dictionary<string, int>> GetPreviousRouteAttemptsAsync(string panelId, string userId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    // If no userId specified, use current user
                    userId = await _authService.GetUserId();

                    if (string.IsNullOrEmpty(userId))
                    {
                        throw new UnauthorizedAccessException("User must be authenticated to get route attempts");
                    }
                }

                // Get all training sessions for this user
                string nodePath = $"trainingSessions/{userId}";
                var sessions = await _databaseService.GetItems<TrainingSession>(nodePath);

                // Filter sessions for the specific panel
                var panelSessions = sessions.Where(s => s.PanelType == panelId).ToList();

                // Dictionary to store route attempts
                Dictionary<string, int> routeAttempts = new Dictionary<string, int>();

                // Process all sessions to sum attempts for each route
                foreach (var session in panelSessions)
                {
                    if (session.CompletedRoutes != null)
                    {
                        foreach (var route in session.CompletedRoutes)
                        {
                            // Sum attempts for all routes, even if not completed
                            if (!routeAttempts.ContainsKey(route.RouteId))
                            {
                                routeAttempts[route.RouteId] = route.Attempts;
                            }
                            else
                            {
                                // Sum the attempts instead of taking the maximum
                                routeAttempts[route.RouteId] += route.Attempts;
                            }
                        }
                    }
                }

                return routeAttempts;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting previous route attempts: {ex.Message}");
                return new Dictionary<string, int>();
            }
        }

    }
}
