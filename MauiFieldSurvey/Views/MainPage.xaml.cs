using MauiFieldSurvey.ViewModels;

namespace MauiFieldSurvey.Views
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}