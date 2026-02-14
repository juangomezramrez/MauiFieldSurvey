using System.Globalization;
using MauiFieldSurvey.Models;

namespace MauiFieldSurvey.Converters
{
    public class JobImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PhotoJob job)
            {
                // Si el trabajo falló, devolvemos una imagen de error (o null para que la UI maneje un fallback)
                if (job.Status == JobStatus.Failed)
                    return "error_icon.png"; // Asegúrate de tener iconos o usa un color en el XAML

                // Si está completada y el archivo existe, mostramos la final
                if (job.Status == JobStatus.Completed && File.Exists(job.FinalImagePath))
                {
                    return ImageSource.FromFile(job.FinalImagePath);
                }

                // Si está pendiente o procesando, intentamos mostrar la Raw (cruda)
                if (File.Exists(job.RawImagePath))
                {
                    return ImageSource.FromFile(job.RawImagePath);
                }
            }

            // Retorno por defecto (null hará que no se muestre nada o se use el FallbackValue del XAML)
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}