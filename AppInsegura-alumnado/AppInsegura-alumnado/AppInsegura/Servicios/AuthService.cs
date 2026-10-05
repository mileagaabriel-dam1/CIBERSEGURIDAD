using System;
using System.Security.Cryptography;
using System.Text;
using AppInsegura.Datos;
using AppInsegura.Modelos;

namespace AppInsegura.Servicios
{
    public class AuthService
    {
        private readonly BaseDatosUsuarios baseDatos;

        public AuthService(BaseDatosUsuarios baseDatos)
        {
            this.baseDatos = baseDatos;
        }

        public Usuario Registrar(string nombre, string contrasena, string rol = "jugador")
        {
            if (string.IsNullOrWhiteSpace(nombre) || baseDatos.BuscarExacto(nombre) != null)
            {
                throw new ArgumentException("Nombre de usuario vacío o ya existente.");
            }
            if (contrasena.Length < 8)
            {
                throw new ArgumentException("La contraseña debe tener al menos 8 caracteres.");
            }
            // ARREGLO: se valida que el nombre no esté vacío ni repetido y que la contraseña tenga mínimo 8 caracteres.
            // Motivo: antes se podían crear usuarios vacíos, duplicar "admin" o usar contraseñas como "1".

            var nuevo = new Usuario
            {
                Nombre = nombre,
                ContrasenaHash = CalcularHash(contrasena),
                Rol = rol,
                TokenSesion = ""
            };

            baseDatos.Agregar(nuevo);
            return nuevo;
        }

        public Usuario? IniciarSesion(string nombre, string contrasena)
        {
            Usuario? usuario = baseDatos.BuscarExacto(nombre);
            if (usuario == null)
            {
                return null;
            }

            if (!VerificarContrasena(contrasena, usuario.ContrasenaHash))
            {
                return null;
            }

            usuario.TokenSesion = GenerarTokenSesion();

            // ARREGLO: quitado el Console.WriteLine("[LOG] ... token: ...").
            // Motivo: mostraba el token de sesión por pantalla; con él se puede robar la sesión.

            // ARREGLO: quitada la llamada a GuardarSesionEnDisco (y el método).
            // Motivo: guardaba usuario y token en "sesion.txt" sin cifrar; cualquiera con acceso al PC podía leerlo.

            return usuario;
        }

        private string CalcularHash(string contrasena)
        {
            byte[] sal = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(contrasena), sal, 100000, HashAlgorithmName.SHA256, 32);
            return Convert.ToHexString(sal) + ":" + Convert.ToHexString(hash);
        }
        // ARREGLO: cambiado MD5 por PBKDF2 (SHA-256, 100.000 iteraciones) con una sal aleatoria por usuario.
        // Se guarda como "sal:hash" en el mismo campo ContrasenaHash.
        // Motivo: MD5 está roto, es muy rápido de romper y sin sal dos contraseñas iguales dan el mismo hash.

        private bool VerificarContrasena(string contrasena, string guardado)
        {
            string[] partes = guardado.Split(':');
            byte[] sal = Convert.FromHexString(partes[0]);
            byte[] hashGuardado = Convert.FromHexString(partes[1]);
            byte[] hashIntento = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(contrasena), sal, 100000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(hashIntento, hashGuardado);
        }
        // ARREGLO: nuevo método que recalcula el hash con la sal guardada y compara con FixedTimeEquals.
        // Motivo: comparar con "!=" tarda distinto según cuántos caracteres coinciden y eso da pistas a un atacante.

        private string GenerarTokenSesion()
        {
            return Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        }
        // ARREGLO: el token se genera con RandomNumberGenerator (64 caracteres) en vez de new Random() con 6 cifras.
        // Motivo: Random es predecible y 6 cifras se pueden adivinar probando; RandomNumberGenerator es seguro.
    }
}