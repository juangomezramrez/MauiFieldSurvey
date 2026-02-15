using SkiaSharp;
using MauiFieldSurvey.Models;

namespace MauiFieldSurvey.Services
{
    public interface IImageProcessingService
    {
        Task ProcessJobAsync(PhotoJob job);
    }

    public class ImageProcessingService : IImageProcessingService
    {
        private readonly IDatabaseService _dbService;

        public ImageProcessingService(IDatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task ProcessJobAsync(PhotoJob job)
        {
            try
            {
                if (!File.Exists(job.RawImagePath))
                {
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = "Archivo Raw no encontrado.";
                    await _dbService.UpdateJobAsync(job);
                    return;
                }

                job.Status = JobStatus.Processing;
                await _dbService.UpdateJobAsync(job);

                // 1. EXTRAER METADATOS: Leemos la orientación EXIF original
                SKEncodedOrigin origin = SKEncodedOrigin.TopLeft;
                using (var stream = File.OpenRead(job.RawImagePath))
                using (var codec = SKCodec.Create(stream))
                {
                    if (codec != null)
                    {
                        origin = codec.EncodedOrigin;
                    }
                }

                // 2. Cargar imagen en crudo
                SKBitmap originalBitmap = SKBitmap.Decode(job.RawImagePath);
                if (originalBitmap == null)
                {
                    throw new Exception("No se pudo decodificar la imagen.");
                }

                // 3. ESTRATEGIA EXIF: Corregimos la rotación basada en el sensor
                originalBitmap = CorrectOrientation(originalBitmap, origin);

                // 4. Bloque using para asegurar liberación de memoria (Evitar OOM)
                using (originalBitmap)
                {
                    int targetWidth = 1280;
                    float ratio = (float)targetWidth / originalBitmap.Width;
                    int targetHeight = (int)(originalBitmap.Height * ratio);

                    if (ratio >= 1)
                    {
                        targetWidth = originalBitmap.Width;
                        targetHeight = originalBitmap.Height;
                    }

                    var info = new SKImageInfo(targetWidth, targetHeight);

                    // FIX: Usamos SKSamplingOptions (La nueva API recomendada de SkiaSharp)
                    using (var resizedBitmap = originalBitmap.Resize(info, new SKSamplingOptions(SKFilterMode.Linear)))
                    using (var canvas = new SKCanvas(resizedBitmap))
                    {
                        DrawWatermark(canvas, job, targetWidth, targetHeight);

                        string finalPath = Path.Combine(FileSystem.AppDataDirectory, $"Final_{Guid.NewGuid()}.jpg");

                        using (var image = SKImage.FromBitmap(resizedBitmap))
                        using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 80))
                        using (var destStream = File.OpenWrite(finalPath))
                        {
                            data.SaveTo(destStream);
                        }

                        job.FinalImagePath = finalPath;
                        job.Status = JobStatus.Completed;
                        job.ErrorMessage = null;

                        await _dbService.UpdateJobAsync(job);
                        File.Delete(job.RawImagePath);
                    }
                }
            }
            catch (Exception ex)
            {
                job.Status = JobStatus.Failed;
                job.ErrorMessage = ex.Message;
                await _dbService.UpdateJobAsync(job);
                Console.WriteLine($"Error procesando imagen: {ex}");
            }
        }

        // NUEVO MÉTODO: Gira los píxeles físicamente según el EXIF
        private SKBitmap CorrectOrientation(SKBitmap bitmap, SKEncodedOrigin origin)
        {
            SKBitmap rotated;
            switch (origin)
            {
                case SKEncodedOrigin.RightTop: // Girado 90° a la derecha (Típico en Android Portrait)
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(rotated.Width, 0);
                        surface.RotateDegrees(90);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }
                    bitmap.Dispose(); // Liberamos el original al instante
                    return rotated;

                case SKEncodedOrigin.LeftBottom: // Girado 270° (90° a la izquierda)
                    rotated = new SKBitmap(bitmap.Height, bitmap.Width);
                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(0, rotated.Height);
                        surface.RotateDegrees(270);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }
                    bitmap.Dispose();
                    return rotated;

                case SKEncodedOrigin.BottomRight: // Girado 180° (Patas arriba)
                    rotated = new SKBitmap(bitmap.Width, bitmap.Height);
                    using (var surface = new SKCanvas(rotated))
                    {
                        surface.Translate(rotated.Width, rotated.Height);
                        surface.RotateDegrees(180);
                        surface.DrawBitmap(bitmap, 0, 0);
                    }
                    bitmap.Dispose();
                    return rotated;

                default:
                    // Si ya está correcta (TopLeft) u otro formato no soportado, devolvemos intacta
                    return bitmap;
            }
        }

        private void DrawWatermark(SKCanvas canvas, PhotoJob job, int width, int height)
        {
            var textSize = width * 0.035f;
            var margin = width * 0.02f;

            // FIX: Utilizamos SKFont en lugar de asignar TextSize al Paint
            using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold);
            using var font = new SKFont(typeface, textSize);

            using var textPaint = new SKPaint
            {
                Color = SKColors.White,
                IsAntialias = true
            };

            using var outlinePaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            string nombre = "Juan Gómez";
            string[] lines = {
                $"Lat: {job.Latitude:F6}",
                $"Lon: {job.Longitude:F6}",
                $"Fecha: {job.Timestamp:yyyy-MM-dd HH:mm:ss}",
                $"Alt: {job.Altitude:F1}m",
                $"@{nombre}"
            };

            float y = height - margin;

            for (int i = lines.Length - 1; i >= 0; i--)
            {
                // FIX: API moderna de DrawText
                canvas.DrawText(lines[i], margin, y, SKTextAlign.Left, font, outlinePaint);
                canvas.DrawText(lines[i], margin, y, SKTextAlign.Left, font, textPaint);

                y -= (textSize * 1.2f);
            }
        }
    }
}

//using SkiaSharp;
//using MauiFieldSurvey.Models;

//namespace MauiFieldSurvey.Services
//{
//    public interface IImageProcessingService
//    {
//        Task ProcessJobAsync(PhotoJob job);
//    }

//    public class ImageProcessingService : IImageProcessingService
//    {
//        private readonly IDatabaseService _dbService;

//        public ImageProcessingService(IDatabaseService dbService)
//        {
//            _dbService = dbService;
//        }

//        public async Task ProcessJobAsync(PhotoJob job)
//        {
//            try
//            {
//                // 1. Validar que el archivo Raw exista
//                if (!File.Exists(job.RawImagePath))
//                {
//                    job.Status = JobStatus.Failed;
//                    job.ErrorMessage = "Archivo Raw no encontrado.";
//                    await _dbService.UpdateJobAsync(job);
//                    return;
//                }

//                // Actualizamos estado a "Procesando"
//                job.Status = JobStatus.Processing;
//                await _dbService.UpdateJobAsync(job);

//                // 2. Cargar y Redimensionar (Evitar OOM)
//                // Usamos un stream para no cargar todo en RAM si es posible
//                using (var stream = File.OpenRead(job.RawImagePath))
//                using (var originalBitmap = SKBitmap.Decode(stream))
//                {
//                    if (originalBitmap == null)
//                    {
//                        throw new Exception("No se pudo decodificar la imagen.");
//                    }

//                    // Calculamos nuevas dimensiones (Max 1280px de ancho para optimizar)
//                    int targetWidth = 1280;
//                    float ratio = (float)targetWidth / originalBitmap.Width;
//                    int targetHeight = (int)(originalBitmap.Height * ratio);

//                    // Si la imagen ya es pequeña, no la agrandamos
//                    if (ratio >= 1)
//                    {
//                        targetWidth = originalBitmap.Width;
//                        targetHeight = originalBitmap.Height;
//                    }

//                    var info = new SKImageInfo(targetWidth, targetHeight);
//                    using (var resizedBitmap = originalBitmap.Resize(info, SKFilterQuality.Medium))
//                    using (var canvas = new SKCanvas(resizedBitmap))
//                    {
//                        // 3. Dibujar Estampado (Watermark)
//                        DrawWatermark(canvas, job, targetWidth, targetHeight);

//                        // 4. Guardar Imagen Final
//                        string finalPath = Path.Combine(FileSystem.AppDataDirectory, $"Final_{Guid.NewGuid()}.jpg");

//                        using (var image = SKImage.FromBitmap(resizedBitmap))
//                        using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 80)) // Calidad 80%
//                        using (var destStream = File.OpenWrite(finalPath))
//                        {
//                            data.SaveTo(destStream);
//                        }

//                        // 5. Actualizar Job y Limpiar
//                        job.FinalImagePath = finalPath;
//                        job.Status = JobStatus.Completed;
//                        job.ErrorMessage = null;

//                        await _dbService.UpdateJobAsync(job);

//                        // BORRADO SEGURO: Solo borramos el Raw si todo salió bien
//                        File.Delete(job.RawImagePath);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                job.Status = JobStatus.Failed;
//                job.ErrorMessage = ex.Message;
//                await _dbService.UpdateJobAsync(job);
//                Console.WriteLine($"Error procesando imagen: {ex}");
//            }
//        }

//        private void DrawWatermark(SKCanvas canvas, PhotoJob job, int width, int height)
//        {
//            // Configuración del Texto
//            var textSize = width * 0.035f; // Tamaño relativo al ancho (3.5%)
//            var margin = width * 0.02f;

//            // Pintura para el texto (Blanco)
//            var textPaint = new SKPaint
//            {
//                Color = SKColors.White,
//                TextSize = textSize,
//                IsAntialias = true,
//                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
//            };

//            // Pintura para el borde del texto (Negro) - Para contraste
//            var outlinePaint = new SKPaint
//            {
//                Color = SKColors.Black,
//                TextSize = textSize,
//                IsAntialias = true,
//                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
//                Style = SKPaintStyle.Stroke,
//                StrokeWidth = 2
//            };

//            // Preparamos las líneas de texto
//            string[] lines = {
//                $"Lat: {job.Latitude:F6}",
//                $"Lon: {job.Longitude:F6}",
//                $"Fecha: {job.Timestamp:yyyy-MM-dd HH:mm:ss}",
//                $"Alt: {job.Altitude:F1}m"
//            };

//            // Dibujar desde la esquina inferior izquierda hacia arriba
//            float y = height - margin;

//            // Dibujamos en orden inverso (de abajo hacia arriba)
//            for (int i = lines.Length - 1; i >= 0; i--)
//            {
//                // Dibujar borde negro
//                canvas.DrawText(lines[i], margin, y, outlinePaint);
//                // Dibujar texto blanco
//                canvas.DrawText(lines[i], margin, y, textPaint);

//                y -= (textSize * 1.2f); // Subir para la siguiente línea
//            }
//        }
//    }
//}