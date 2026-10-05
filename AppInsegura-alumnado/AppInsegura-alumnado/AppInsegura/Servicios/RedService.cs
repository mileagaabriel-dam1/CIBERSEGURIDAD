using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AppInsegura.Servicios
{
    public class RedService
    {
        private const string NombreVariableApiKey = "MIAPP_API_KEY";
        // ARREGLO: se elimina la constante  ApiKey = "sk_live_51Hj29aKlmZ9QwErTyUiOpAsDfGh"  y se lee de una variable de entorno.
        // MOTIVO: era un SECRETO ESCRITO EN EL CÓDIGO. Cualquiera con el código (o descompilando el .exe, o en GitHub) podía usar la clave.
        //         Los secretos van en variables de entorno o en un gestor de secretos, nunca en el código fuente.

        private const string UrlServidor = "https://api.miapp-insegura.local/puntuaciones";
        // ARREGLO: la URL pasa de "http://" a "https://".
        // MOTIVO: por HTTP todo viaja sin cifrar; cualquiera en la misma red (wifi pública, etc.) podía leer o modificar los datos y la clave.

        private static readonly HttpClient cliente = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
        // ARREGLO: un único HttpClient compartido (static) con un tiempo máximo de espera de 10 segundos.
        // MOTIVO: crear un HttpClient en cada llamada agota los sockets del sistema, y sin timeout la app se podía quedar colgada.

        public void EnviarPuntuacion(string nombreUsuario, int puntuacion)
        {
            string? apiKey = Environment.GetEnvironmentVariable(NombreVariableApiKey);
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.WriteLine($"Sincronización no disponible: falta configurar la variable de entorno {NombreVariableApiKey}.");
                return;
            }
            // ARREGLO: si la clave no está configurada, no se intenta la conexión y se avisa.
            // MOTIVO: fallar de forma segura (fail-safe) en vez de enviar peticiones sin credenciales.

            if (puntuacion < 0 || puntuacion > 1_000_000)
            {
                Console.WriteLine("Puntuación no válida.");
                return;
            }
            // ARREGLO: se valida que la puntuación esté en un rango razonable antes de enviarla.
            // MOTIVO: no se deben enviar al servidor datos sin validar.

            try
            {
                EnviarPuntuacionAsync(nombreUsuario, puntuacion, apiKey).GetAwaiter().GetResult();
            }
            catch (Exception)
            {
                Console.WriteLine("No se ha podido sincronizar con el servidor. Inténtalo más tarde.");
            }
            // ARREGLO: ya no se muestra ex.Message al usuario, solo un mensaje genérico.
            // MOTIVO: el mensaje técnico de la excepción revela detalles internos (servidor, DNS, rutas...) útiles para un atacante.
        }

        private async Task EnviarPuntuacionAsync(string nombreUsuario, int puntuacion, string apiKey)
        {
            using var peticion = new HttpRequestMessage(HttpMethod.Post, UrlServidor);
            peticion.Headers.Add("X-Api-Key", apiKey);
            peticion.Content = JsonContent.Create(new { usuario = nombreUsuario, puntos = puntuacion });
            // ARREGLO: se usa POST con los datos en un cuerpo JSON y la clave en una cabecera HTTP, en lugar de un GET con todo en la URL.
            // MOTIVO: 1) Las URLs se guardan en logs de servidores, proxies e historial: la api_key quedaba expuesta.
            //         2) El nombre de usuario se pegaba en la URL sin codificar: con "&" o "=" se podían inyectar parámetros extra.
            //         3) GET no debe usarse para operaciones que modifican datos (guardar una puntuación).

            // ARREGLO: se elimina  Console.WriteLine($"Enviando puntuación a: {url}");
            // MOTIVO: imprimía la URL completa, INCLUIDA LA API KEY, por pantalla.

            using HttpResponseMessage respuesta = await cliente.SendAsync(peticion);
            Console.WriteLine(respuesta.IsSuccessStatusCode
                ? "Puntuación sincronizada correctamente."
                : "El servidor ha rechazado la sincronización.");
            // ARREGLO: se muestra un mensaje comprensible en lugar del código HTTP en bruto.
            // MOTIVO: no dar información técnica innecesaria al usuario.
        }
    }
}