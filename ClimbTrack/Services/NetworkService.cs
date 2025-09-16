using ClimbTrack.Exceptions;
using ClimbTrack.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace ClimbTrack.Services
{
    public interface INetworkService
    {
        Task<Result<T>> GetAsync<T>(string url, string context = null);
        Task<Result<T>> PostAsync<T>(string url, object data, string context = null);
        Task<Result> PostAsync(string url, object data, string context = null);
    }

    public class NetworkService : INetworkService
    {
        private readonly HttpClient _httpClient;
        private readonly IErrorHandlingService _errorHandlingService;
        private readonly IConnectivity _connectivity;

        public NetworkService(IErrorHandlingService errorHandlingService, IConnectivity connectivity)
        {
            _httpClient = new HttpClient();
            _errorHandlingService = errorHandlingService;
            _connectivity = connectivity;

            // Set default headers, timeouts, etc.
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<Result<T>> GetAsync<T>(string url, string context = null)
        {
            try
            {
                // Check network connectivity
                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    var ex = new NetworkException("No internet connection", NetworkConnectivity.NoConnection);
                    await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                    return Result<T>.Failure(ex, ClimbTrack.Models.ErrorSeverity.Warning);
                }

                // Make the request
                var response = await _httpClient.GetAsync(url);

                // Check for success
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<T>();
                    return Result<T>.Success(result);
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var ex = new HttpRequestException($"HTTP error {(int)response.StatusCode}: {content}");
                    await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                    return Result<T>.Failure(ex);
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                return Result<T>.Failure(ex);
            }
        }

        public async Task<Result<T>> PostAsync<T>(string url, object data, string context = null)
        {
            try
            {
                // Check network connectivity
                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    var ex = new NetworkException("No internet connection", NetworkConnectivity.NoConnection);
                    await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                    return Result<T>.Failure(ex, ClimbTrack.Models.ErrorSeverity.Warning);
                }

                // Make the request
                var response = await _httpClient.PostAsJsonAsync(url, data);

                // Check for success
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<T>();
                    return Result<T>.Success(result);
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var ex = new HttpRequestException($"HTTP error {(int)response.StatusCode}: {content}");
                    await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                    return Result<T>.Failure(ex);
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                return Result<T>.Failure(ex);
            }
        }

        public async Task<Result> PostAsync(string url, object data, string context = null)
        {
            try
            {
                // Check network connectivity
                if (_connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    var ex = new NetworkException("No internet connection", NetworkConnectivity.NoConnection);
                    await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                    return Result.Failure(ex, ClimbTrack.Models.ErrorSeverity.Warning);
                }

                // Make the request
                var response = await _httpClient.PostAsJsonAsync(url, data);

                // Check for success
                if (response.IsSuccessStatusCode)
                {
                    return Result.Success();
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var ex = new HttpRequestException($"HTTP error {(int)response.StatusCode}: {content}");
                    await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                    return Result.Failure(ex);
                }
            }
            catch (Exception ex)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, context, true);
                return Result.Failure(ex);
            }
        }
    }
}