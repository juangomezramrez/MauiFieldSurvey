using Microsoft.Extensions.Logging;
using MauiFieldSurvey.Services;
using MauiFieldSurvey.Models;
using MauiFieldSurvey.ViewModels; // Agregar
using MauiFieldSurvey.Views;      // Agregar
using SkiaSharp.Views.Maui.Controls.Hosting;
using MauiFieldSurvey.Controls;


namespace MauiFieldSurvey
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
                    // AQUÍ REGISTRAMOS EL HANDLER (Compilación Condicional)
#if ANDROID
                    handlers.AddHandler(typeof(NativeCameraView), typeof(Platforms.Android.NativeCameraViewHandler));
#endif
                }); 

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Servicios
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<IGeoLocationService, GeoLocationService>();

            // ViewModels y Views
            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<MainPage>();

            // NUEVO: Registrar el procesador de imágenes
            builder.Services.AddSingleton<IImageProcessingService, ImageProcessingService>();

            // NUEVO: ViewModels y Views (Página de Detalle)
            // Transient porque se crea y destruye cada vez que entramos
            builder.Services.AddTransient<JobDetailViewModel>();
            builder.Services.AddTransient<JobDetailPage>();

            return builder.Build();
        }
    }
}