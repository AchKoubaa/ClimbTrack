using ClimbTrack.ViewModels;

namespace ClimbTrack.Views
{
    public partial class SessionDetailsPage : ContentPage
    {
        private readonly SessionDetailsViewModel _viewModel;

        public SessionDetailsPage(SessionDetailsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Puoi aggiungere qui eventuali inizializzazioni necessarie
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Puoi eseguire operazioni di pulizia qui se necessario
        }

        protected override bool OnBackButtonPressed()
        {
            // Utilizziamo il comando GoBack del ViewModel
            _viewModel.GoBackCommand.Execute(null);
            return true; // Preveniamo il comportamento predefinito
        }
    }
}