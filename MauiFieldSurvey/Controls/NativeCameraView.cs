

namespace MauiFieldSurvey.Controls
{
    // Este es el control que pondremos en nuestro XAML. 
    // En Windows será un control vacío, en Android será el PreviewView de CameraX.
    public class NativeCameraView : View
    {
        // Evento para solicitar al Handler nativo que tome la foto
        public event EventHandler TakePhotoRequested;

        // Evento que el Handler disparará cuando la foto se guarde en disco
        public event EventHandler<string> PhotoCaptured;

        // Evento en caso de error
        public event EventHandler<string> CaptureFailed;

        public void CapturePhoto()
        {
            TakePhotoRequested?.Invoke(this, EventArgs.Empty);
        }

        public void OnPhotoCaptured(string filePath)
        {
            PhotoCaptured?.Invoke(this, filePath);
        }

        public void OnCaptureFailed(string errorMessage)
        {
            CaptureFailed?.Invoke(this, errorMessage);
        }
    }
}