using ClimbTrack.Models;
using ClimbTrack.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClimbTrack.ViewModels
{
    public class BaseViewModel : BaseModel
    {
        private bool _isBusy;
        private string _title;
        private bool _isErrorVisible;
        private string _errorMessage;

        protected readonly IErrorHandlingService _errorHandlingService;

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public bool IsErrorVisible
        {
            get => _isErrorVisible;
            set => SetProperty(ref _isErrorVisible, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand RetryCommand { get; }
        public ICommand DismissErrorCommand { get; }

        public BaseViewModel(IErrorHandlingService errorHandlingService = null)
        {
            _errorHandlingService = errorHandlingService;
            RetryCommand = new Command(async () => await RetryLastOperation());
            DismissErrorCommand = new Command(ClearError);
        }

        protected bool SetBusy(bool value)
        {
            return SetProperty(ref _isBusy, value);
        }

        protected async Task ExecuteWithBusy(Func<Task> action)
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();
                await action();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, nameof(ExecuteWithBusy));
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Store the last operation for retry functionality
        private Func<Task> _lastOperation;

        // Enhanced version with error handling and retry capability
        protected async Task ExecuteWithErrorHandlingAsync(Func<Task> operation, string context = null)
        {
            _lastOperation = operation;

            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                ClearError();
                await operation();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, context);
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Generic version that returns a value
        protected async Task<T> ExecuteWithErrorHandlingAsync<T>(Func<Task<T>> operation, string context = null, T defaultValue = default)
        {
            if (IsBusy)
                return defaultValue;

            try
            {
                IsBusy = true;
                ClearError();
                return await operation();
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, context);
                return defaultValue;
            }
            finally
            {
                IsBusy = false;
            }
        }

        // Version that returns a Result object
        protected async Task<Result<T>> ExecuteWithResultAsync<T>(Func<Task<T>> operation, string context = null)
        {
            if (IsBusy)
                return Result<T>.Failure("Operation already in progress");

            try
            {
                IsBusy = true;
                ClearError();
                var result = await operation();
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(ex, context, false);
                return Result<T>.Failure(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        protected async Task HandleExceptionAsync(Exception ex, string context = null, bool showToUser = true)
        {
            // Log the error
            System.Diagnostics.Debug.WriteLine($"ERROR [{context ?? "Unknown"}]: {ex.Message}");

            // Use error handling service if available
            if (_errorHandlingService != null)
            {
                await _errorHandlingService.HandleExceptionAsync(ex, context, showToUser);
            }

            // Show in the UI
            ShowError(GetUserFriendlyMessage(ex));
        }

        protected void ShowError(string message)
        {
            ErrorMessage = message;
            IsErrorVisible = true;
        }

        protected void ClearError()
        {
            ErrorMessage = null;
            IsErrorVisible = false;
        }

        protected async Task RetryLastOperation()
        {
            if (_lastOperation != null)
            {
                ClearError();
                await ExecuteWithErrorHandlingAsync(_lastOperation);
            }
        }

        protected string GetUserFriendlyMessage(Exception ex)
        {
            // If we have an error handling service, use it
            if (_errorHandlingService != null)
            {
                return _errorHandlingService.GetUserFriendlyMessage(ex);
            }

            // Otherwise, provide basic error messages
            return ex switch
            {
                System.Net.Http.HttpRequestException _ => "Network connection issue. Please check your internet connection.",
                TimeoutException _ => "The operation timed out. Please try again.",
                UnauthorizedAccessException _ => "You don't have permission to perform this action.",
                Firebase.Database.FirebaseException firebaseEx when firebaseEx.Message.Contains("Permission denied") =>
                    "You don't have permission to access this data. Please log in again.",
                _ => "An unexpected error occurred. Please try again later."
            };
        }
    }

    // Add this Result class if you don't have it already
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string ErrorMessage { get; }
        public Exception Exception { get; }

        private Result(bool isSuccess, T value, string errorMessage, Exception exception)
        {
            IsSuccess = isSuccess;
            Value = value;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result<T> Success(T value) => new Result<T>(true, value, null, null);
        public static Result<T> Failure(string errorMessage) => new Result<T>(false, default, errorMessage, null);
        public static Result<T> Failure(Exception ex) => new Result<T>(false, default, ex.Message, ex);
    }
}
