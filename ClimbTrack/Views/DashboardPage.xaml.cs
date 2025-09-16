using ClimbTrack.ViewModels;
using Microcharts;
using Microcharts.Maui;
using SkiaSharp;

namespace ClimbTrack.Views;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OnAppearingAsync();
    }
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Puoi eseguire operazioni di pulizia qui se necessario
    }
}