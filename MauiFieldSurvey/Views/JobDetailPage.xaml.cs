using MauiFieldSurvey.ViewModels;

namespace MauiFieldSurvey.Views
{
    public partial class JobDetailPage : ContentPage
    {
        public JobDetailPage(JobDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel;
        }
    }
}