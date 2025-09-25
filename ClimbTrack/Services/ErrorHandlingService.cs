using ClimbTrack.Models;
using Firebase.Database;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    // Define the missing enum
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    
    public class ErrorHandlingService : IErrorHandlingService
    {
        private readonly IAnalyticsService _analyticsService;
        private readonly IConnectivity _connectivity;
        private readonly INavigationService _navigationService;
        private bool _analyticsInitialized = false;
        public ErrorHandlingService(
            IAnalyticsService analyticsService = null,
            IConnectivity connectivity = null, 
            INavigationService navigationService = null)
        {
            _analyticsService = analyticsService;
            _connectivity = connectivity ?? Connectivity.Current;
            _navigationService = navigationService;
        }

        public async Task InitializeAnalyticsAsync(string appId)
        {
            if (_analyticsService != null && !_analyticsInitialized)
            {
                await _analyticsService.InitializeAsync(appId);
                _analyticsInitialized = true;
            }

        }
        // Add this new method
        public async Task HandleAuthenticationExceptionAsync(Exception ex, string context = null)
        {
            // Log the error
            LogErrorInternal(ex, context, ErrorSeverity.Critical);

            // Track in analytics if available
            if (_analyticsService != null)
            {
                var properties = new Dictionary<string, string>
                {
                    ["exception_type"] = ex.GetType().Name,
                    ["exception_message"] = ex.Message,
                    ["context"] = context ?? "Unknown"
                };

                await _analyticsService.TrackEventAsync("authentication_error", properties);
            }

            // Show a user-friendly message
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Application.Current?.MainPage != null)
                {
                    bool goToLogin = await Shell.Current.DisplayAlert(
                        "Authentication Required",
                        "You need to sign in to continue. Would you like to sign in now?",
                        "Yes", "No");

                    if (goToLogin && _navigationService != null)
                    {
                        // Navigate to login page
                        await Shell.Current.GoToAsync("login");
                    }
                }
            });
        }


        public async Task HandleExceptionAsync(Exception ex, string context = null, bool showToUser = true)
        {
            // Determine error severity
            var severity = DetermineErrorSeverity(ex);

            // Log the error
            LogErrorInternal(ex, context, severity);

            // Track in analytics if available
            if (_analyticsService != null && _analyticsInitialized)
            {
                await _analyticsService.TrackErrorAsync(ex, context);
            }

            // Show to user if needed
            if (showToUser)
            {
                await ShowErrorToUserAsync(GetUserFriendlyMessage(ex), GetErrorTitle(severity));
            }
        }

        public async Task LogErrorAsync(string message, string context = null, bool showToUser = false)
        {
            // Log the error message
            Debug.WriteLine($"ERROR [{context ?? "Unknown"}]: {message}");

            // Track in analytics if available
            if (_analyticsService != null && _analyticsInitialized)
            {
                await _analyticsService.TrackEventAsync("Error", new Dictionary<string, string>
                {
                    ["Message"] = message,
                    ["Context"] = context ?? "Unknown"
                });
            }

            // Show to user if needed
            if (showToUser)
            {
                await ShowErrorToUserAsync(message);
            }
        }

        public async Task<bool> HandleHttpErrorAsync(HttpResponseMessage response, string context = null)
        {
            if (response.IsSuccessStatusCode)
                return true;

            var statusCode = (int)response.StatusCode;
            var reasonPhrase = response.ReasonPhrase;
            var content = await response.Content.ReadAsStringAsync();

            var message = $"HTTP Error {statusCode}: {reasonPhrase}";
            var ex = new HttpRequestException(message);

            await HandleExceptionAsync(ex, context, true);
            return false;
        }

        public async Task ShowErrorToUserAsync(string message, string title = "Error")
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (Application.Current?.MainPage != null)
                {
                    await Shell.Current.DisplayAlert(title, message, "OK");
                }
                else
                {
                    Debug.WriteLine($"Cannot show error to user: {message}");
                }
            });
        }

        public string GetUserFriendlyMessage(Exception ex)
        {
            return ex switch
            {
                HttpRequestException _ when !_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet) =>
                    "No internet connection. Please check your network settings and try again.",

                HttpRequestException _ =>
                    "We're having trouble connecting to our servers. Please try again later.",

                TimeoutException _ =>
                    "The operation is taking longer than expected. Please try again.",

                UnauthorizedAccessException _ =>
                    "You don't have permission to perform this action. Please log in again.",

                FirebaseException firebaseEx when firebaseEx.Message.Contains("Permission denied") =>
                    "You don't have permission to access this data. Please log in again.",

                FirebaseException _ =>
                    "There was an issue connecting to the database. Please try again later.",

                FormatException _ =>
                    "There was an issue processing some data. Please try again.",

                _ => "An unexpected error occurred. Please try again later."
            };
        }

        private ErrorSeverity DetermineErrorSeverity(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException _ => ErrorSeverity.Critical,
                HttpRequestException _ when !_connectivity.NetworkAccess.HasFlag(NetworkAccess.Internet) => ErrorSeverity.Warning,
                HttpRequestException _ => ErrorSeverity.Error,
                TimeoutException _ => ErrorSeverity.Warning,
                FirebaseException firebaseEx when firebaseEx.Message.Contains("Permission denied") => ErrorSeverity.Critical,
                FirebaseException _ => ErrorSeverity.Error,
                _ => ErrorSeverity.Error
            };
        }

        private string GetErrorTitle(ErrorSeverity severity)
        {
            return severity switch
            {
                ErrorSeverity.Critical => "Critical Error",
                ErrorSeverity.Error => "Error",
                ErrorSeverity.Warning => "Warning",
                ErrorSeverity.Info => "Information",
                _ => "Error"
            };
        }

        private void LogErrorInternal(Exception ex, string context, ErrorSeverity severity)
        {
            var errorData = new Dictionary<string, string>
            {
                ["Exception"] = ex.GetType().Name,
                ["Message"] = ex.Message,
                ["StackTrace"] = ex.StackTrace,
                ["Context"] = context ?? "Unknown",
                ["Severity"] = severity.ToString(),
                ["Timestamp"] = DateTime.UtcNow.ToString("o"),
                ["DeviceInfo"] = DeviceInfo.Current.Model,
                ["Platform"] = DeviceInfo.Current.Platform.ToString(),
                ["AppVersion"] = AppInfo.Current.VersionString,
                ["NetworkAccess"] = _connectivity.NetworkAccess.ToString()
            };

            // Log to debug output
            Debug.WriteLine($"ERROR [{severity}] [{context ?? "Unknown"}]: {ex.Message}");
            Debug.WriteLine($"Details: {string.Join(", ", errorData.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

            // In a real app, you might log to a file or remote logging service
        }
    }
}