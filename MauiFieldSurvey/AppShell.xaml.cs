using MauiFieldSurvey.Views;

namespace MauiFieldSurvey
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // REGISTRO DE RUTA DE NAVEGACIÓN
            Routing.RegisterRoute(nameof(JobDetailPage), typeof(JobDetailPage));
            Routing.RegisterRoute(nameof(CameraPage), typeof(CameraPage)); // Faltaba esta línea
        }
    }
}