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
            
            // hide the tab bar when this page appears
            Shell.SetTabBarIsVisible(this, false);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Optional: Restore the tab bar when leaving this page
           
            Shell.SetTabBarIsVisible(this, true);
        }

        protected override bool OnBackButtonPressed()
        {
            // Utilizziamo il comando GoBack del ViewModel
            _viewModel.GoBackCommand.Execute(null);
            return true; // Preveniamo il comportamento predefinito
        }
    }
}