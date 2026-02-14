using SQLite;
using System;

namespace MauiFieldSurvey.Models
{
    // Definimos los estados posibles del proceso para manejar fallos y reintentos
    public enum JobStatus
    {
        Pending,    // Foto tomada, guardada en temporal, pero no procesada
        Processing, // Actualmente siendo redimensionada/etiquetada
        Completed,  // Foto final lista, temporal borrado
        Failed      // Falló el procesamiento, requiere reintento manual o automático
    }

    [Table("PhotoJobs")]
    public class PhotoJob
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // Ruta de la imagen "Cruda" (Raw). 
        // Es la foto tal cual sale de la cámara, pesada y sin editar.
        // Si el proceso falla, esta es nuestra copia de seguridad.
        public string RawImagePath { get; set; }

        // Ruta de la imagen final procesada (redimensionada y con sello).
        public string FinalImagePath { get; set; }

        // Metadatos geográficos y temporales (Capturados al instante de tomar la foto)
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double Altitude { get; set; }
        public DateTime Timestamp { get; set; }

        // Texto que el usuario o el sistema añade a la foto
        public string UserCaption { get; set; }

        // Control de Estado para la estrategia "Paranoica"
        public JobStatus Status { get; set; }

        // Mensaje de error si falló (útil para depurar en campo)
        public string ErrorMessage { get; set; }

        public PhotoJob()
        {
            Timestamp = DateTime.Now;
            Status = JobStatus.Pending;
        }
    }
}