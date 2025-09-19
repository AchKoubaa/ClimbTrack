using ClimbTrack.ViewModels;

namespace ClimbTrack.Views
{
    public partial class EditProfilePage : ContentPage
    {
        private readonly EditProfileViewModel _viewModel;

        public EditProfilePage(EditProfileViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Explicitly hide the tab bar and nav bar when this page appears
            Shell.SetTabBarIsVisible(this, false);
            
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Optional: Restore the tab bar and nav bar when leaving this page
            // Only needed if you're experiencing issues with the bars not reappearing
            Shell.SetTabBarIsVisible(this, true);
            // Shell.SetNavBarIsVisible(this, true);
        }
    }
}