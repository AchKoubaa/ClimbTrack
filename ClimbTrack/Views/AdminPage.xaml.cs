using ClimbTrack.ViewModels;

namespace ClimbTrack.Views
{
    public partial class AdminPage : ContentPage
    {
        public AdminPage(AdminViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}