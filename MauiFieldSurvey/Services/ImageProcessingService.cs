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
                // 1. Validar que el archivo Raw exista
                if (!File.Exists(job.RawImagePath))
                {
                    job.Status = JobStatus.Failed;
                    job.ErrorMessage = "Archivo Raw no encontrado.";
                    await _dbService.UpdateJobAsync(job);
                    return;
                }

                // Actualizamos estado a "Procesando"
                job.Status = JobStatus.Processing;
                await _dbService.UpdateJobAsync(job);

                // 2. Cargar y Redimensionar (Evitar OOM)
                // Usamos un stream para no cargar todo en RAM si es posible
                using (var stream = File.OpenRead(job.RawImagePath))
                using (var originalBitmap = SKBitmap.Decode(stream))
                {
                    if (originalBitmap == null)
                    {
                        throw new Exception("No se pudo decodificar la imagen.");
                    }

                    // Calculamos nuevas dimensiones (Max 1280px de ancho para optimizar)
                    int targetWidth = 1280;
                    float ratio = (float)targetWidth / originalBitmap.Width;
                    int targetHeight = (int)(originalBitmap.Height * ratio);

                    // Si la imagen ya es pequeña, no la agrandamos
                    if (ratio >= 1)
                    {
                        targetWidth = originalBitmap.Width;
                        targetHeight = originalBitmap.Height;
                    }

                    var info = new SKImageInfo(targetWidth, targetHeight);
                    using (var resizedBitmap = originalBitmap.Resize(info, SKFilterQuality.Medium))
                    using (var canvas = new SKCanvas(resizedBitmap))
                    {
                        // 3. Dibujar Estampado (Watermark)
                        DrawWatermark(canvas, job, targetWidth, targetHeight);

                        // 4. Guardar Imagen Final
                        string finalPath = Path.Combine(FileSystem.AppDataDirectory, $"Final_{Guid.NewGuid()}.jpg");

                        using (var image = SKImage.FromBitmap(resizedBitmap))
                        using (var data = image.Encode(SKEncodedImageFormat.Jpeg, 80)) // Calidad 80%
                        using (var destStream = File.OpenWrite(finalPath))
                        {
                            data.SaveTo(destStream);
                        }

                        // 5. Actualizar Job y Limpiar
                        job.FinalImagePath = finalPath;
                        job.Status = JobStatus.Completed;
                        job.ErrorMessage = null;

                        await _dbService.UpdateJobAsync(job);

                        // BORRADO SEGURO: Solo borramos el Raw si todo salió bien
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

        private void DrawWatermark(SKCanvas canvas, PhotoJob job, int width, int height)
        {
            // Configuración del Texto
            var textSize = width * 0.035f; // Tamaño relativo al ancho (3.5%)
            var margin = width * 0.02f;

            // Pintura para el texto (Blanco)
            var textPaint = new SKPaint
            {
                Color = SKColors.White,
                TextSize = textSize,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };

            // Pintura para el borde del texto (Negro) - Para contraste
            var outlinePaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = textSize,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2
            };

            // Preparamos las líneas de texto
            string[] lines = {
                $"Lat: {job.Latitude:F6}",
                $"Lon: {job.Longitude:F6}",
                $"Fecha: {job.Timestamp:yyyy-MM-dd HH:mm:ss}",
                $"Alt: {job.Altitude:F1}m"
            };

            // Dibujar desde la esquina inferior izquierda hacia arriba
            float y = height - margin;

            // Dibujamos en orden inverso (de abajo hacia arriba)
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                // Dibujar borde negro
                canvas.DrawText(lines[i], margin, y, outlinePaint);
                // Dibujar texto blanco
                canvas.DrawText(lines[i], margin, y, textPaint);

                y -= (textSize * 1.2f); // Subir para la siguiente línea
            }
        }
    }
}