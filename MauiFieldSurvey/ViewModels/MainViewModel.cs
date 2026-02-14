using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiFieldSurvey.Models;
using MauiFieldSurvey.Services;
using MauiFieldSurvey.Views;
using System.Collections.ObjectModel;

namespace MauiFieldSurvey.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDatabaseService _dbService;
        private readonly IGeoLocationService _geoService;
        private readonly IImageProcessingService _imgService; // NUEVO

        [ObservableProperty]
        ObservableCollection<PhotoJob> _jobs;

        [ObservableProperty]
        bool _isBusy;

        [ObservableProperty]
        string _statusMessage;

        // Inyectamos el servicio de imágenes
        public MainViewModel(IDatabaseService dbService, IGeoLocationService geoService, IImageProcessingService imgService)
        {
            _dbService = dbService;
            _geoService = geoService;
            _imgService = imgService;
            Jobs = new ObservableCollection<PhotoJob>();

            // Cargar trabajos y procesar pendientes al inicio
            LoadAndProcessJobsCommand.Execute(null);
        }

        [RelayCommand]
        async Task LoadAndProcessJobs()
        {
            IsBusy = true;
            try
            {
                // 1. Cargar lista desde BD
                var list = await _dbService.GetAllJobsAsync();
                Jobs.Clear();
                foreach (var job in list)
                {
                    Jobs.Add(job);
                }

                // 2. Buscar pendientes y procesarlos en segundo plano
                var pending = list.Where(x => x.Status == JobStatus.Pending || x.Status == JobStatus.Failed).ToList();

                if (pending.Any())
                {
                    StatusMessage = $"Procesando {pending.Count} fotos...";
                    foreach (var job in pending)
                    {
                        await _imgService.ProcessJobAsync(job);

                        // Refrescar item en la lista (truco rápido para actualizar UI)
                        var index = Jobs.IndexOf(job);
                        if (index >= 0) Jobs[index] = job;
                    }
                    StatusMessage = "Procesamiento completado.";
                }
                else
                {
                    StatusMessage = "Listo.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        //[RelayCommand]
        //async Task TakePhoto()
        //{
        //    if (IsBusy) return;

        //    try
        //    {
        //        IsBusy = true;
        //        StatusMessage = "Obteniendo GPS...";

        //        var location = await _geoService.GetCurrentLocationAsync();
        //        if (location == null) location = new Location(0, 0);

        //        StatusMessage = "Abriendo Cámara...";

        //        if (MediaPicker.Default.IsCaptureSupported)
        //        {
        //            FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

        //            if (photo != null)
        //            {
        //                StatusMessage = "Guardando...";

        //                string localFileName = Path.Combine(FileSystem.AppDataDirectory, $"{Guid.NewGuid()}.jpg");

        //                using (Stream sourceStream = await photo.OpenReadAsync())
        //                using (FileStream localFileStream = File.OpenWrite(localFileName))
        //                {
        //                    await sourceStream.CopyToAsync(localFileStream);
        //                }

        //                var newJob = new PhotoJob
        //                {
        //                    RawImagePath = localFileName,
        //                    Status = JobStatus.Pending,
        //                    Latitude = location.Latitude,
        //                    Longitude = location.Longitude,
        //                    Altitude = location.Altitude ?? 0,
        //                    Timestamp = DateTime.Now
        //                };

        //                await _dbService.AddJobAsync(newJob);
        //                Jobs.Insert(0, newJob);

        //                StatusMessage = "Procesando foto...";

        //                // INICIO DEL PROCESO PARANOICO:
        //                // Llamamos al procesador inmediatamente, pero de forma asíncrona
        //                // para que la UI se actualice mientras trabaja.
        //                await _imgService.ProcessJobAsync(newJob);

        //                // Forzamos actualización visual del item
        //                var index = Jobs.IndexOf(newJob);
        //                if (index >= 0) Jobs[index] = newJob;

        //                StatusMessage = "Foto Completada.";
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        StatusMessage = $"Error: {ex.Message}";
        //        await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        //    }
        //    finally
        //    {
        //        IsBusy = false;
        //    }
        //}
        // ... (código anterior)

        [RelayCommand]
        async Task TakePhoto()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Obteniendo GPS...";

                var location = await _geoService.GetCurrentLocationAsync();
                if (location == null) location = new Location(0, 0);

                StatusMessage = "Abriendo Cámara...";

                if (MediaPicker.Default.IsCaptureSupported)
                {
                    // La cámara guarda esto en CacheDirectory temporalmente
                    FileResult photo = await MediaPicker.Default.CapturePhotoAsync();

                    if (photo != null)
                    {
                        StatusMessage = "Guardando...";

                        // Ruta Segura (AppData)
                        string localFileName = Path.Combine(FileSystem.AppDataDirectory, $"{Guid.NewGuid()}.jpg");

                        // COPIAR: De Cache -> AppData
                        using (Stream sourceStream = await photo.OpenReadAsync())
                        using (FileStream localFileStream = File.OpenWrite(localFileName))
                        {
                            await sourceStream.CopyToAsync(localFileStream);
                        }

                        // NUEVO: BORRAR EL ORIGINAL DE CACHÉ INMEDIATAMENTE
                        // Ya tenemos la copia segura, el original es basura.
                        try
                        {
                            // FileResult.FullPath apunta al archivo en caché
                            if (File.Exists(photo.FullPath))
                            {
                                File.Delete(photo.FullPath);
                            }
                        }
                        catch
                        {
                            // Si falla borrar el temporal (ej. Android lo retiene un momento), 
                            // no pasa nada, lo borrará el 'OnStart' la próxima vez.
                            Console.WriteLine("No se pudo borrar el temporal inmediatamente.");
                        }

                        // ... (Resto del código: crear Job, guardar en BD, procesar...)

                        var newJob = new PhotoJob
                        {
                            RawImagePath = localFileName,
                            Status = JobStatus.Pending,
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            Altitude = location.Altitude ?? 0,
                            Timestamp = DateTime.Now
                        };

                        await _dbService.AddJobAsync(newJob);
                        Jobs.Insert(0, newJob);

                        StatusMessage = "Procesando foto...";

                        await _imgService.ProcessJobAsync(newJob);

                        var index = Jobs.IndexOf(newJob);
                        if (index >= 0) Jobs[index] = newJob;

                        StatusMessage = "Foto Completada.";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                await App.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
        [RelayCommand]
        async Task JobTapped(PhotoJob job)
        {
            if (job == null) return;

            // Navegamos a la página de detalle pasando el objeto Job
            var navigationParameter = new Dictionary<string, object>
            {
                { "Job", job }
            };

            await Shell.Current.GoToAsync(nameof(JobDetailPage), navigationParameter);
        }

    }
}