using ClimbTrack.Models;
using ClimbTrack.ViewModels;
using System;
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
                // For now, just navigate to the session details page without passing the session
                // We'll fix the navigation issue later
                await Shell.Current.GoToAsync($"//sessionDetails?id={session.Id}&userId={session.UserId}");

                // Deselect the item
                if (sender is CollectionView collectionView)
                {
                    collectionView.SelectedItem = null;
                }
            }
        }
    }
}