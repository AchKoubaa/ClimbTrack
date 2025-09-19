using ClimbTrack.Models;
using ClimbTrack.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;

namespace ClimbTrack.Views
{
    public partial class HistoricalPage : ContentPage
    {
        private readonly HistoricalViewModel _viewModel;

        public HistoricalPage(HistoricalViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.Initialize();
        }

        private async void OnFilterButtonClicked(object sender, EventArgs e)
        {
            // Create a simple filter popup
            string panel = await DisplayPromptAsync("Filtro Pannello", "Inserisci il tipo di pannello:", "OK", "Annulla", placeholder: "Es. Moonboard");

            if (!string.IsNullOrEmpty(panel))
            {
                _viewModel.FilterPanel = panel;
            }
        }

        private async void OnSessionSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is TrainingSession session)
            {
                try
                {
                    // Deselect the item first to prevent multiple selections
                    if (sender is CollectionView collectionView)
                    {
                        collectionView.SelectedItem = null;
                    }

                    
                    await Shell.Current.GoToAsync("sessionDetails", new Dictionary<string, object>
            {
                { "id", session.Id },
                { "userId", session.UserId }
            });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Navigation error: {ex.Message}");
                    // Show an error message to the user
                    await Shell.Current.DisplayAlert("Navigation Error",
                        "Unable to open session details. Please try again.", "OK");
                }
            }
        }
    }
}