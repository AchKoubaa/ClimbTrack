using ClimbTrack.Services;
using ClimbTrack.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace ClimbTrack.Views;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _viewModel;
    private IErrorHandlingService _errorHandlingService;

    public LoginPage(LoginViewModel viewModel, IErrorHandlingService errorHandlingService)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _errorHandlingService = errorHandlingService;
        BindingContext = _viewModel;

        // Subscribe to property changed event to handle error messages
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Get the error handling service from the service provider
        _errorHandlingService = IPlatformApplication.Current?.Services.GetService<IErrorHandlingService>();

        try
        {
            await _viewModel.CheckAuthenticationStatus();
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(ex, "Authentication check failed");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Unsubscribe to prevent memory leaks
        _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }

    private async void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // If the ErrorMessage property changes and is not empty, show it to the user
        if (e.PropertyName == nameof(LoginViewModel.ErrorMessage) &&
            !string.IsNullOrEmpty(_viewModel.ErrorMessage))
        {
            // Get the error message and clear it from the view model
            string errorMessage = _viewModel.ErrorMessage;
            _viewModel.ErrorMessage = string.Empty;

            // Show the error using the error handling service
            if (_errorHandlingService != null)
            {
                await _errorHandlingService.ShowErrorToUserAsync(errorMessage);
            }
        }
    }

    private async Task HandleErrorAsync(Exception ex, string context)
    {
        if (_errorHandlingService != null)
        {
            // Use the error handling service to handle the exception
            await _errorHandlingService.HandleExceptionAsync(ex, context, true);
        }
        else
        {
            // Fallback if service is not available
            await DisplayAlert("Error", "An error occurred. Please try again.", "OK");
        }
    }
}