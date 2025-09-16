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

            // Subscribe to connectivity changes
            _connectivity.ConnectivityChanged += Connectivity_ConnectivityChanged;
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
                    _logger.LogInformation("No internet connection available, queuing analytics event: {EventName}", eventName);
                    // Optionally queue the event for later sending
                    QueueEventForLaterSending(eventName, properties);
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

                // Add timeout to prevent long-hanging requests
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

                await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/events", payload);
                _logger.LogDebug("Tracked event: {EventName}", eventName);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network error while tracking event: {EventName}. Will queue for later.", eventName);
                // Queue the event for later sending
                QueueEventForLaterSending(eventName, properties);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Request timeout while tracking event: {EventName}. Will queue for later.", eventName);
                // Queue the event for later sending
                QueueEventForLaterSending(eventName, properties);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to track event: {EventName}", eventName);
            }
        }
        private void QueueEventForLaterSending(string eventName, Dictionary<string, string> properties)
        {
            try
            {
                // Simple implementation: store in local storage
                // In a real app, you might use a more sophisticated queuing mechanism
                var queuedEvents = GetQueuedEvents();
                queuedEvents.Add(new QueuedEvent
                {
                    EventName = eventName,
                    Properties = properties,
                    Timestamp = DateTimeOffset.UtcNow
                });
                SaveQueuedEvents(queuedEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue event for later sending: {EventName}", eventName);
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

        // Simple class to represent a queued event
        private class QueuedEvent
        {
            public string EventName { get; set; }
            public Dictionary<string, string> Properties { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }

        // Get previously queued events from storage
        private List<QueuedEvent> GetQueuedEvents()
        {
            try
            {
                var json = Preferences.Get("queued_analytics_events", null);
                if (string.IsNullOrEmpty(json))
                    return new List<QueuedEvent>();

                return System.Text.Json.JsonSerializer.Deserialize<List<QueuedEvent>>(json)
                    ?? new List<QueuedEvent>();
            }
            catch
            {
                return new List<QueuedEvent>();
            }
        }

        // Save queued events to storage
        private void SaveQueuedEvents(List<QueuedEvent> events)
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(events);
                Preferences.Set("queued_analytics_events", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save queued events");
            }
        }

        public async Task ProcessQueuedEventsAsync()
        {
            if (_connectivity.NetworkAccess != NetworkAccess.Internet)
            {
                _logger.LogInformation("No internet connection available, skipping processing of queued events");
                return;
            }

            try
            {
                var queuedEvents = GetQueuedEvents();
                if (queuedEvents.Count == 0)
                    return;

                _logger.LogInformation("Processing {Count} queued analytics events", queuedEvents.Count);

                // Process events in batches to avoid overwhelming the network
                foreach (var batch in queuedEvents.Chunk(10))
                {
                    foreach (var queuedEvent in batch)
                    {
                        try
                        {
                            var payload = new
                            {
                                Type = "event",
                                EventName = queuedEvent.EventName,
                                Properties = queuedEvent.Properties ?? new Dictionary<string, string>(),
                                Timestamp = queuedEvent.Timestamp,
                                AppId = _appId,
                                DeviceId = _deviceId,
                                IsQueued = true
                            };

                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await _httpClient.PostAsJsonAsync($"{_apiEndpoint}/events", payload, cts.Token);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to process queued event: {EventName}", queuedEvent.EventName);
                            // Continue with other events even if one fails
                        }

                        // Small delay to avoid overwhelming the network
                        await Task.Delay(100);
                    }
                }

                // Clear successfully processed events
                SaveQueuedEvents(new List<QueuedEvent>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing queued events");
            }
        }

        private void Connectivity_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                // Network is available, process queued events
                _ = ProcessQueuedEventsAsync();
            }
        }

        // Don't forget to unsubscribe when the service is disposed
        public void Dispose()
        {
            _connectivity.ConnectivityChanged -= Connectivity_ConnectivityChanged;
            _httpClient?.Dispose();
        }
    }
}
