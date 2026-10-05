using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AppInsegura.Servicios
{
    public class RedService
    {
        private readonly string? ApiKey = Environment.GetEnvironmentVariable("API_KEY");
        // ARREGLO: la API key ya no está escrita en el código; se lee de la variable de entorno API_KEY.
        // Motivo: una clave en el código la puede ver cualquiera que tenga el código o lo suba a GitHub.

        private const string UrlServidor = "https://api.miapp-insegura.local/puntuaciones";
        // ARREGLO: cambiado http:// por https://.
        // Motivo: por HTTP los datos viajan sin cifrar y cualquiera en la misma red puede leerlos.

        public void EnviarPuntuacion(string nombreUsuario, int puntuacion)
        {
            if (ApiKey == null)
            {
                Console.WriteLine("Falta configurar la variable de entorno API_KEY.");
                return;
            }

            try
            {
                EnviarPuntuacionAsync(nombreUsuario, puntuacion).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                Console.WriteLine("No se ha podido conectar con el servidor.");
            }
            // ARREGLO: quitado Console.WriteLine(ex.Message).
            // Motivo: el mensaje técnico del error revela detalles internos del servidor.
        }

        private async Task EnviarPuntuacionAsync(string nombreUsuario, int puntuacion)
        {
            using var cliente = new HttpClient();
            cliente.DefaultRequestHeaders.Add("X-Api-Key", ApiKey);
            string url = $"{UrlServidor}?usuario={Uri.EscapeDataString(nombreUsuario)}&puntos={puntuacion}";
            // ARREGLO: la API key va en una cabecera y no en la URL; el nombre de usuario se codifica con Uri.EscapeDataString.
            // Motivo: las URLs se guardan en logs e historiales (la clave quedaba expuesta), y sin codificar
            // un nombre con "&" o "=" podía añadir parámetros falsos a la petición.

            // ARREGLO: quitado Console.WriteLine($"Enviando puntuación a: {url}").
            // Motivo: mostraba la URL completa con la API key por pantalla.

            HttpResponseMessage respuesta = await cliente.GetAsync(url);
            Console.WriteLine($"Respuesta del servidor: {(int)respuesta.StatusCode}");
        }
    }
}