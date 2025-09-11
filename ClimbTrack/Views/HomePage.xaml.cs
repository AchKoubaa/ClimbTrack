using ClimbTrack.ViewModels;

namespace ClimbTrack.Views
{
    public partial class HomePage : ContentPage
    {
        private readonly HomeViewModel _viewModel;

        public HomePage(HomeViewModel viewModel)
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

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Puoi eseguire operazioni di pulizia qui se necessario
        }
    }
}