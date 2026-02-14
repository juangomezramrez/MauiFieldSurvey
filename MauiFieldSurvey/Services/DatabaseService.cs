using SQLite;
using MauiFieldSurvey.Models;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MauiFieldSurvey.Services
{
    public interface IDatabaseService
    {
        Task<int> AddJobAsync(PhotoJob job);
        Task<int> UpdateJobAsync(PhotoJob job);
        Task<List<PhotoJob>> GetPendingJobsAsync();
        Task<List<PhotoJob>> GetAllJobsAsync();
        Task<int> DeleteJobAsync(PhotoJob job);
    }

    public class DatabaseService : IDatabaseService
    {
        private SQLiteAsyncConnection _database;

        // Inicialización Lazy de la base de datos
        private async Task Init()
        {
            if (_database != null)
                return;

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "FieldSurvey.db3");

            // Flags para máxima compatibilidad y seguridad de datos
            var flags = SQLiteOpenFlags.ReadWrite |
                        SQLiteOpenFlags.Create |
                        SQLiteOpenFlags.SharedCache;

            _database = new SQLiteAsyncConnection(dbPath, flags);

            // Crea la tabla si no existe
            await _database.CreateTableAsync<PhotoJob>();
        }

        public async Task<int> AddJobAsync(PhotoJob job)
        {
            await Init();
            return await _database.InsertAsync(job);
        }

        public async Task<int> UpdateJobAsync(PhotoJob job)
        {
            await Init();
            return await _database.UpdateAsync(job);
        }

        public async Task<List<PhotoJob>> GetPendingJobsAsync()
        {
            await Init();
            // Recuperamos trabajos que no estén completados
            return await _database.Table<PhotoJob>()
                                  .Where(j => j.Status == JobStatus.Pending || j.Status == JobStatus.Failed)
                                  .ToListAsync();
        }

        public async Task<List<PhotoJob>> GetAllJobsAsync()
        {
            await Init();
            // Ordenamos por fecha descendente para ver las últimas primero
            return await _database.Table<PhotoJob>()
                                  .OrderByDescending(x => x.Timestamp)
                                  .ToListAsync();
        }

        public async Task<int> DeleteJobAsync(PhotoJob job)
        {
            await Init();
            return await _database.DeleteAsync(job);
        }
    }
}