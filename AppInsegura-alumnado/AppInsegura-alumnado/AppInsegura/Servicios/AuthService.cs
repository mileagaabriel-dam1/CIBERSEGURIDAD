using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using AppInsegura.Datos;
using AppInsegura.Modelos;
// ARREGLO: se elimina "using System.IO".
// MOTIVO: ya no se escribe la sesión en un fichero de disco (ver más abajo).

namespace AppInsegura.Servicios
{
    public class AuthService
    {
        private readonly BaseDatosUsuarios baseDatos;

        private const int TamanoSal = 16;
        private const int TamanoHash = 32;
        private const int Iteraciones = 600_000;
        private static readonly HashAlgorithmName AlgoritmoHash = HashAlgorithmName.SHA256;
        // ARREGLO: parámetros para PBKDF2 (sal de 16 bytes, hash de 32 bytes, 600.000 iteraciones con SHA-256).
        // MOTIVO: son los valores recomendados por OWASP; las iteraciones hacen que cada intento de adivinar la contraseña sea lento y caro.

        private const int MaxIntentosFallidos = 5;
        private static readonly TimeSpan DuracionBloqueo = TimeSpan.FromMinutes(5);
        // ARREGLO: límite de 5 intentos fallidos y bloqueo de la cuenta durante 5 minutos.
        // MOTIVO: antes se podía probar contraseñas infinitamente (fuerza bruta).

        private static readonly Regex PatronNombre = new Regex("^[a-zA-Z0-9_]{3,20}$");
        // ARREGLO: patrón de lista blanca para los nombres de usuario (solo letras, números y guion bajo, de 3 a 20 caracteres).
        // MOTIVO: antes se aceptaba cualquier cosa (vacío, comillas, saltos de línea...), lo que permitía inyecciones y falsear mensajes/logs.

        private readonly byte[] salFicticia = RandomNumberGenerator.GetBytes(TamanoSal);
        // ARREGLO: sal ficticia para calcular un hash aunque el usuario no exista.
        // MOTIVO: así el login tarda lo mismo exista o no el usuario, y no se puede averiguar qué usuarios existen midiendo tiempos.

        public AuthService(BaseDatosUsuarios baseDatos)
        {
            this.baseDatos = baseDatos;
        }

        public Usuario Registrar(string nombre, string contrasena)
        {
            return CrearUsuario(nombre, contrasena, Roles.Jugador);
        }
        // ARREGLO: el registro público ya no recibe el parámetro "rol"; siempre crea usuarios con rol "jugador".
        // MOTIVO: antes el método aceptaba cualquier rol, y cualquier código que lo llamara podía crear administradores (escalada de privilegios).

        public Usuario RegistrarAdministrador(string nombre, string contrasena)
        {
            return CrearUsuario(nombre, contrasena, Roles.Admin);
        }
        // ARREGLO: método separado y explícito para crear administradores (solo se usa al arrancar la aplicación).
        // MOTIVO: separar ambos caminos deja claro dónde se conceden privilegios y evita darlos por accidente.

        private Usuario CrearUsuario(string nombre, string contrasena, string rol)
        {
            nombre = (nombre ?? "").Trim();

            if (!PatronNombre.IsMatch(nombre))
            {
                throw new ValidacionException("El nombre de usuario debe tener entre 3 y 20 caracteres y solo puede contener letras, números y '_'.");
            }
            // ARREGLO: se valida el nombre de usuario con la lista blanca antes de guardarlo.
            // MOTIVO: "nunca confíes en la entrada del usuario": antes se podían registrar nombres vacíos o con caracteres peligrosos.

            ValidarContrasena(nombre, contrasena);
            // ARREGLO: se exige una contraseña robusta.
            // MOTIVO: antes valía cualquier contraseña, incluso vacía o "1234".

            if (baseDatos.BuscarExacto(nombre) != null)
            {
                throw new ValidacionException("Ese nombre de usuario no está disponible.");
            }
            // ARREGLO: se comprueba que el nombre no esté ya en uso.
            // MOTIVO: antes se podía registrar otro "admin" duplicado.

            byte[] sal = RandomNumberGenerator.GetBytes(TamanoSal);
            // ARREGLO: se genera una sal aleatoria criptográficamente segura para cada usuario.
            // MOTIVO: hace que el mismo password genere hashes distintos en cada usuario e inutiliza las tablas arcoíris.

            var nuevo = new Usuario
            {
                Nombre = nombre,
                Sal = Convert.ToBase64String(sal),
                ContrasenaHash = Convert.ToBase64String(CalcularHash(contrasena, sal)),
                Rol = rol,
                TokenSesion = ""
            };

            baseDatos.Agregar(nuevo);
            return nuevo;
        }

        private static void ValidarContrasena(string nombre, string contrasena)
        {
            if (string.IsNullOrEmpty(contrasena) || contrasena.Length < 8 || contrasena.Length > 128)
            {
                throw new ValidacionException("La contraseña debe tener entre 8 y 128 caracteres.");
            }
            if (!Regex.IsMatch(contrasena, "[a-zA-Z]") || !Regex.IsMatch(contrasena, "[0-9]"))
            {
                throw new ValidacionException("La contraseña debe contener al menos una letra y un número.");
            }
            if (contrasena.Contains(nombre, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidacionException("La contraseña no puede contener el nombre de usuario.");
            }
        }
        // ARREGLO: nueva política de contraseñas (longitud mínima 8, letras y números, sin incluir el nombre de usuario).
        // MOTIVO: contraseñas como "admin1234" o "ana2024" se adivinan en segundos con un diccionario.

        public Usuario? IniciarSesion(string nombre, string contrasena)
        {
            Usuario? usuario = baseDatos.BuscarExacto(nombre ?? "");
            if (usuario == null)
            {
                CalcularHash(contrasena ?? "", salFicticia);
                return null;
            }
            // ARREGLO: si el usuario no existe se calcula igualmente un hash (que se descarta).
            // MOTIVO: antes se respondía al instante si el usuario no existía y tardaba más si existía; midiendo el tiempo se sabía qué usuarios hay.

            if (usuario.BloqueadoHasta.HasValue && usuario.BloqueadoHasta.Value > DateTime.UtcNow)
            {
                CalcularHash(contrasena ?? "", salFicticia);
                return null;
            }
            // ARREGLO: si la cuenta está bloqueada por demasiados intentos, se rechaza el login aunque la contraseña sea correcta.
            // MOTIVO: frena los ataques de fuerza bruta.

            byte[] sal = Convert.FromBase64String(usuario.Sal);
            byte[] hashIntento = CalcularHash(contrasena ?? "", sal);
            byte[] hashGuardado = Convert.FromBase64String(usuario.ContrasenaHash);

            if (!CryptographicOperations.FixedTimeEquals(hashIntento, hashGuardado))
            {
                usuario.IntentosFallidos++;
                if (usuario.IntentosFallidos >= MaxIntentosFallidos)
                {
                    usuario.BloqueadoHasta = DateTime.UtcNow.Add(DuracionBloqueo);
                    usuario.IntentosFallidos = 0;
                }
                return null;
            }
            // ARREGLO: los hashes se comparan con CryptographicOperations.FixedTimeEquals en lugar de "!=".
            // MOTIVO: "!=" se para en el primer carácter distinto, y por el tiempo de respuesta se puede deducir el hash (ataque de temporización).
            // ARREGLO: se cuentan los fallos y se bloquea la cuenta al llegar a 5.
            // MOTIVO: protección contra fuerza bruta.

            usuario.IntentosFallidos = 0;
            usuario.BloqueadoHasta = null;
            usuario.TokenSesion = GenerarTokenSesion();

            // ARREGLO: se elimina  Console.WriteLine($"[LOG] Login correcto -> usuario: ..., token: {usuario.TokenSesion}");
            // MOTIVO: mostraba el token de sesión por pantalla. Quien lo viera (o leyera los logs) podía secuestrar la sesión.
            //         Los logs nunca deben contener contraseñas, tokens ni claves.

            // ARREGLO: se elimina la llamada a GuardarSesionEnDisco(usuario) y el propio método.
            // MOTIVO: guardaba "usuario:token" en texto plano en "sesion.txt"; cualquiera con acceso al equipo podía leerlo y robar la sesión.

            return usuario;
        }

        public void CerrarSesion(Usuario usuario)
        {
            usuario.TokenSesion = "";
        }
        // ARREGLO: nuevo método para cerrar sesión e invalidar el token.
        // MOTIVO: antes no había forma de cerrar sesión, así que el token seguía siendo válido para siempre.

        private static byte[] CalcularHash(string contrasena, byte[] sal)
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(contrasena),
                sal,
                Iteraciones,
                AlgoritmoHash,
                TamanoHash);
        }
        // ARREGLO: se sustituye MD5 por PBKDF2 (Rfc2898DeriveBytes.Pbkdf2) con SHA-256, sal y 600.000 iteraciones.
        // MOTIVO: MD5 está roto y es extremadamente rápido (miles de millones de intentos por segundo con una GPU),
        //         además no usaba sal. PBKDF2 es un algoritmo pensado para guardar contraseñas: lento a propósito y con sal.

        private static string GenerarTokenSesion()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToHexString(bytes);
        }
        // ARREGLO: el token se genera con RandomNumberGenerator (32 bytes = 256 bits) en lugar de new Random() con 6 cifras.
        // MOTIVO: System.Random no es criptográficamente seguro (es predecible) y 6 cifras son solo 900.000 posibilidades,
        //         que se prueban en segundos. Un token de 256 bits aleatorios es imposible de adivinar.
    }

    public class ValidacionException : Exception
    {
        public ValidacionException(string mensaje) : base(mensaje) { }
    }
    // ARREGLO: nueva excepción propia para los errores de validación.
    // MOTIVO: permite enseñar al usuario SOLO nuestros mensajes controlados, y ocultar el detalle de cualquier otro error interno.
}