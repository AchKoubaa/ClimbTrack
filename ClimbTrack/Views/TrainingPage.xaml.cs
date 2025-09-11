using ClimbTrack.ViewModels;

namespace ClimbTrack.Views
{
    public partial class TrainingPage : ContentPage
    {
        private readonly TrainingViewModel _viewModel;

        public TrainingPage(TrainingViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Verifica se ci sono percorsi disponibili
            if (_viewModel.TrainingRoutes == null || _viewModel.TrainingRoutes.Count == 0)
            {
                DisplayAlert("Attenzione", "Non ci sono percorsi disponibili per questo allenamento.", "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Assicurati che il timer venga fermato quando la pagina viene chiusa
            if (_viewModel.IsTrainingActive)
            {
                _viewModel.EndTrainingCommand.Execute(null);
            }
        }

       

        protected override bool OnBackButtonPressed()
        {
            // Chiedi conferma prima di uscire se l'allenamento è attivo
            if (_viewModel.IsTrainingActive)
            {
                Device.BeginInvokeOnMainThread(async () =>
                {
                    bool shouldExit = await DisplayAlert(
                        "Allenamento in corso",
                        "Sei sicuro di voler uscire? L'allenamento verrà terminato senza salvare.",
                        "Esci", "Annulla");

                    if (shouldExit)
                    {
                        await Navigation.PopAsync();
                    }
                });
                return true; // Impedisce il comportamento predefinito del pulsante indietro
            }

            return base.OnBackButtonPressed();
        }
    }
}