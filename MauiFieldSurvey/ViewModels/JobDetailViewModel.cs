using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiFieldSurvey.Models;


namespace MauiFieldSurvey.ViewModels
{
    // IQueryAttributable permite recibir datos de navegación
    public partial class JobDetailViewModel : ObservableObject, IQueryAttributable
    {
        [ObservableProperty]
        PhotoJob _job;

        [ObservableProperty]
        ImageSource _imageSource;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            // Recibimos el objeto "Job" desde la navegación
            if (query.ContainsKey("Job") && query["Job"] is PhotoJob receivedJob)
            {
                Job = receivedJob;
                LoadImage();
            }
        }

        private void LoadImage()
        {
            if (Job == null) return;

            // Lógica similar al converter: Priorizar Final, luego Raw
            string path = Job.Status == JobStatus.Completed ? Job.FinalImagePath : Job.RawImagePath;

            if (File.Exists(path))
            {
                ImageSource = ImageSource.FromFile(path);
            }
        }

        [RelayCommand]
        async Task GoBack()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}