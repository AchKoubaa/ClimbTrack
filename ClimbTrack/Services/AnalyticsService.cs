using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    /// <summary>
    /// Simple implementation of IAnalyticsService for MAUI applications
    /// </summary>
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ILogger<AnalyticsService> _logger;
        private readonly HttpClient _httpClient;
        private readonly string _apiEndpoint;
        private readonly IConnectivity _connectivity;
        private string _appId;
        private string _deviceId;
        private bool _isInitialized;

        /// <summary>
        /// Initializes a new instance of the AnalyticsService
        /// </summary>
        /// <param name="logger">Logger for recording service operations</param>
        /// <param name="connectivity">Connectivity service to check network status</param>
        public AnalyticsService(
            ILogger<AnalyticsService> logger,
            IConnectivity connectivity)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));

            _httpClient = new HttpClient();
            _apiEndpoint = "https://api.youranalytics.com/v1"; // Replace with your actual endpoint
        }

        /// <inheritdoc />
        public async Task InitializeAsync(string appId)
        {
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentException("App ID cannot be null or empty", nameof(appId));
            }

            _appId = appId;

            // Generate or retrieve a persistent device ID
            _deviceId = await GetOrCreateDeviceIdAsync();

            // Set common headers for all requests
            _httpClient.DefaultRequestHeaders.Add("X-App-Id", _appId);
            _httpClient.DefaultRequestHeaders.Add("X-Device-Id", _deviceId);

            _isInitialized = true;

            // Track app start event
            await TrackEventAsync("app_start", new Dictionary<string, string>
            {
                ["app_version"] = AppInfo.VersionString,
                ["platform"] = DeviceInfo.Platform.ToString(),
                ["device_model"] = DeviceInfo.Model
            });

            _logger.LogInformation("Analytics service initialized with app ID: {AppId}", appId);
        }

        /// <inheritdoc />
        public async Task TrackEventAsync(string eventName, Dictionary<string, string> properties = null)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(eventName))
            {
                throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
            }

            try
            {
                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogInformation("No internet connection available, skipping analytics");
                    return;
                }

                var payload = new
                {
                    Type = "event",
                    EventName = eventName,
                    Properties = properties ?? new Dictionary<string, string>(),
                    Timestamp = DateTimeOffset.UtcNow,
                    AppId = _appId,
                    DeviceId = _deviceId
                };

                await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/events", payload);
                _logger.LogDebug("Tracked event: {EventName}", eventName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track event: {EventName}", eventName);
            }
        }

        /// <inheritdoc />
        public async Task TrackPageViewAsync(string pageName, Dictionary<string, string> properties = null)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(pageName))
            {
                throw new ArgumentException("Page name cannot be null or empty", nameof(pageName));
            }

            try
            {
                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogInformation("No internet connection available, skipping analytics");
                    return;
                }

                var payload = new
                {
                    Type = "pageview",
                    PageName = pageName,
                    Properties = properties ?? new Dictionary<string, string>(),
                    Timestamp = DateTimeOffset.UtcNow,
                    AppId = _appId,
                    DeviceId = _deviceId
                };

                await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/pageviews", payload);
                _logger.LogDebug("Tracked page view: {PageName}", pageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track page view: {PageName}", pageName);
            }
        }

        /// <inheritdoc />
        public async Task IdentifyUserAsync(string userId, Dictionary<string, string> traits = null)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
            }

            try
            {
                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogInformation("No internet connection available, skipping analytics");
                    return;
                }

                var payload = new
                {
                    Type = "identify",
                    UserId = userId,
                    Traits = traits ?? new Dictionary<string, string>(),
                    Timestamp = DateTimeOffset.UtcNow,
                    AppId = _appId,
                    DeviceId = _deviceId
                };

                await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/identify", payload);

                // Store the user ID for future events
                await SecureStorage.SetAsync("analytics_user_id", userId);

                _logger.LogInformation("Identified user: {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to identify user: {UserId}", userId);
            }
        }

        private async Task<string> GetOrCreateDeviceIdAsync()
        {
            try
            {
                // Try to get existing device ID
                var deviceId = await SecureStorage.GetAsync("analytics_device_id");

                if (string.IsNullOrEmpty(deviceId))
                {
                    // Create a new device ID
                    deviceId = Guid.NewGuid().ToString();
                    await SecureStorage.SetAsync("analytics_device_id", deviceId);
                }

                return deviceId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting/creating device ID");
                // Fallback to a temporary ID
                return $"temp_{Guid.NewGuid()}";
            }
        }

        private void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Analytics service must be initialized with InitializeAsync before use");
            }
        }

        public async Task TrackErrorAsync(Exception exception, string context = null)
        {
            EnsureInitialized();

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            try
            {
                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    _logger.LogInformation("No internet connection available, skipping analytics");
                    return;
                }

                var properties = new Dictionary<string, string>
                {
                    ["error_type"] = exception.GetType().Name,
                    ["error_message"] = exception.Message,
                    ["stack_trace"] = exception.StackTrace ?? "No stack trace",
                    ["context"] = context ?? "Unknown"
                };

                if (exception.InnerException != null)
                {
                    properties["inner_error_type"] = exception.InnerException.GetType().Name;
                    properties["inner_error_message"] = exception.InnerException.Message;
                }

                var payload = new
                {
                    Type = "error",
                    Properties = properties,
                    Timestamp = DateTimeOffset.UtcNow,
                    AppId = _appId,
                    DeviceId = _deviceId
                };

                await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/errors", payload);
                _logger.LogDebug("Tracked error: {ErrorType}", exception.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track error");
            }
        }
    }
}
