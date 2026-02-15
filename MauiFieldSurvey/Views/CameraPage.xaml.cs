using MauiFieldSurvey.Services;
using MauiFieldSurvey.Models;

namespace MauiFieldSurvey.Views;

public partial class CameraPage : ContentPage
{
    private readonly IDatabaseService _dbService;
    private readonly IGeoLocationService _geoService;
    private readonly IImageProcessingService _imgService;
    private Location _currentLocation;

    public CameraPage(IDatabaseService dbService, IGeoLocationService geoService, IImageProcessingService imgService)
    {
        InitializeComponent();
        _dbService = dbService;
        _geoService = geoService;
        _imgService = imgService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        lblStatus.Text = "Obteniendo GPS...";
        _currentLocation = await _geoService.GetCurrentLocationAsync() ?? new Location(0, 0);
        lblStatus.Text = "GPS Listo. Puede capturar.";
    }

    private void BtnCancel_Clicked(object sender, EventArgs e)
    {
        Shell.Current.GoToAsync("..");
    }

    private void BtnCapture_Clicked(object sender, EventArgs e)
    {
        lblStatus.Text = "Procesando...";

#if ANDROID
        // En Android, el handler nativo toma la foto
        cameraView.CapturePhoto();
#else
        // Opcional: Manejo fallback para Windows si quisieras llamar a MediaPicker desde aquí.
        lblStatus.Text = "Esta vista es nativa de Android.";
#endif
    }

    //private async void CameraView_PhotoCaptured(object sender, string e)
    //{
    //    // 'e' contiene la ruta de la foto temporal en caché
    //    string localFileName = Path.Combine(FileSystem.AppDataDirectory, $"{Guid.NewGuid()}.jpg");

    //    // Mover a nuestro directorio seguro
    //    File.Copy(e, localFileName, true);
    //    File.Delete(e); // Borramos el de caché inmediato

    //    var newJob = new PhotoJob
    //    {
    //        RawImagePath = localFileName,
    //        Status = JobStatus.Pending,
    //        Latitude = _currentLocation.Latitude,
    //        Longitude = _currentLocation.Longitude,
    //        Altitude = _currentLocation.Altitude ?? 0,
    //        Timestamp = DateTime.Now
    //    };

    //    await _dbService.AddJobAsync(newJob);
    //    _ = Task.Run(() => _imgService.ProcessJobAsync(newJob));

    //    await Shell.Current.GoToAsync("..");
    //}

    private async void CameraView_PhotoCaptured(object sender, string e)
    {
        // 1. Mover archivo a directorio seguro local
        string localFileName = Path.Combine(FileSystem.AppDataDirectory, $"{Guid.NewGuid()}.jpg");
        File.Copy(e, localFileName, true);

        try
        {
            File.Delete(e); // Limpieza inmediata del caché
        }
        catch { /* Ignorar si el SO lo retiene un instante */ }

        // 2. Crear Job en estado Pending
        var newJob = new PhotoJob
        {
            RawImagePath = localFileName,
            Status = JobStatus.Pending,
            Latitude = _currentLocation.Latitude,
            Longitude = _currentLocation.Longitude,
            Altitude = _currentLocation.Altitude ?? 0,
            Timestamp = DateTime.Now
        };

        await _dbService.AddJobAsync(newJob);

        // 3. ESTRATEGIA ESTRICTA: Sin tareas en segundo plano.
        // Solo regresamos a la pantalla principal. 
        // El método OnAppearing() de la MainPage detectará el estado 'Pending' y lo procesará de forma segura.
        await Shell.Current.GoToAsync("..");
    }


    private void CameraView_CaptureFailed(object sender, string e)
    {
        lblStatus.Text = $"Error: {e}";
    }
}