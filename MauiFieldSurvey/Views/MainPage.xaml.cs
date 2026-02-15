using MauiFieldSurvey.ViewModels;

namespace MauiFieldSurvey.Views
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel _viewModel;

        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();

            // Guardamos una referencia al ViewModel para poder usarlo después
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        // Este método se ejecuta automáticamente CADA VEZ que la pantalla se hace visible
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Le decimos al ViewModel: "¡Hey! Acabamos de volver, actualiza la lista 
            // y revisa si hay fotos pendientes de procesar"
            if (_viewModel.LoadAndProcessJobsCommand.CanExecute(null))
            {
                _viewModel.LoadAndProcessJobsCommand.Execute(null);
            }
        }
    }
}
//using MauiFieldSurvey.ViewModels;

//namespace MauiFieldSurvey.Views
//{
//    public partial class MainPage : ContentPage
//    {
//        public MainPage(MainViewModel viewModel)
//        {
//            InitializeComponent();
//            BindingContext = viewModel;
//        }
//    }
//}