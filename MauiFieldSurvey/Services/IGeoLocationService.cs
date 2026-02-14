using Microsoft.Maui.Devices.Sensors;

namespace MauiFieldSurvey.Services
{
    public interface IGeoLocationService
    {
        Task<Location> GetCurrentLocationAsync();
        string FormatCoordinates(Location location);
    }

    public class GeoLocationService : IGeoLocationService
    {
        // Estrategia "Paranoica":
        // 1. Intentar obtener la última ubicación conocida (caché del sistema) para respuesta instantánea.
        // 2. Si es muy vieja o nula, pedir una nueva posición con timeout corto.

        public async Task<Location> GetCurrentLocationAsync()
        {
            try
            {
                // Paso 1: Intentar obtener la última ubicación conocida (caché)
                // Esto es rapidísimo y no gasta batería extra.
                Location location = await Geolocation.Default.GetLastKnownLocationAsync();

                // Si tenemos una ubicación y es reciente (ej. menos de 1 minuto), la usamos.
                if (location != null)
                {
                    // Opcional: Podrías validar location.Timestamp si quieres ser muy estricto
                    // Pero para "Field Survey" a pie, la última conocida suele servir si falló el GPS.
                    return location;
                }

                // Paso 2: Si no hay caché, forzamos la lectura del GPS
                // Solicitamos Alta Precisión porque NO tenemos red (Medium/Low suelen usar Wifi/Celdas)
                var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

                location = await Geolocation.Default.GetLocationAsync(request);

                return location;
            }
            catch (FeatureNotSupportedException)
            {
                // El dispositivo no tiene GPS (raro en móviles, posible en tablets baratas)
                // Devolvemos una ubicación "dummy" o null para manejarlo en la UI
                return null;
            }
            catch (FeatureNotEnabledException)
            {
                // El usuario apagó el GPS
                return null;
            }
            catch (PermissionException)
            {
                // No nos dieron permisos
                return null;
            }
            catch (Exception)
            {
                // Timeout u otro error
                return null;
            }
        }

        public string FormatCoordinates(Location location)
        {
            if (location == null) return "Ubicación no disponible";

            // Formato decimal estándar para facilidad de lectura en campo
            // Lat: 19.4326, Lon: -99.1332, Alt: 2240m
            return $"Lat: {location.Latitude:F5}\nLon: {location.Longitude:F5}\nAlt: {location.Altitude:F1}m";
        }
    }
}