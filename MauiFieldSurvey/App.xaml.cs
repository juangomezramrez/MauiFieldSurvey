using Microsoft.Extensions.DependencyInjection;
namespace MauiFieldSurvey
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Asignamos el Shell como página principal  .Microsoft.Maui.Media.MediaPicker
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
            base.OnStart();

            // ESTRATEGIA PARANOICA: Limpieza de arranque
            // Liberamos espacio borrando residuos de sesiones anteriores
            CleanCacheDirectory();
        }

        private void CleanCacheDirectory()
        {
            Task.Run(() =>
            {
                try
                {
                    var cacheDir = FileSystem.CacheDirectory;

                    if (Directory.Exists(cacheDir))
                    {
                        // ESTRATEGIA PARANOICA ACTUALIZADA:
                        // SearchOption.AllDirectories obliga a buscar dentro de subcarpetas 
                        // ocultas como '.Microsoft.Maui.Media.MediaPicker'
                        var files = Directory.GetFiles(cacheDir, "*.*", SearchOption.AllDirectories);

                        int deletedCount = 0;
                        foreach (var file in files)
                        {
                            try
                            {
                                var extension = Path.GetExtension(file).ToLower();

                                // Filtramos estrictamente por extensiones para no borrar 
                                // cachés importantes del sistema o de la red.
                                if (extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".tmp")
                                {
                                    File.Delete(file);
                                    deletedCount++;
                                }
                            }
                            catch
                            {
                                // Si un archivo está bloqueado por el SO, lo ignoramos en este ciclo
                            }
                        }
                        Console.WriteLine($"[Limpieza] Caché profundo limpiado: {deletedCount} imágenes fantasma eliminadas.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Limpieza] Error limpiando caché profundo: {ex.Message}");
                }
            });
        }

        //private void CleanCacheDirectory()
        //{
        //    Task.Run(() =>
        //    {
        //        try
        //        {
        //            var cacheDir = FileSystem.CacheDirectory;

        //            if (Directory.Exists(cacheDir))
        //            {
        //                var files = Directory.GetFiles(cacheDir);
        //                foreach (var file in files)
        //                {
        //                    try
        //                    {
        //                        // Solo borramos imágenes o temporales, para no borrar configs si las hubiera
        //                        if (file.EndsWith(".jpg") || file.EndsWith(".png") || file.EndsWith(".tmp"))
        //                        {
        //                            File.Delete(file);
        //                        }
        //                    }
        //                    catch
        //                    {
        //                        // Si un archivo está bloqueado, lo ignoramos y seguimos
        //                    }
        //                }
        //                Console.WriteLine($"[Limpieza] Caché limpiado: {files.Length} archivos revisados.");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"[Limpieza] Error limpiando caché: {ex.Message}");
        //        }
        //    });
        //}

    }
}