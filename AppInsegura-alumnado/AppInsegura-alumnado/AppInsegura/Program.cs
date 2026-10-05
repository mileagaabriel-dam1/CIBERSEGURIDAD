using System;
using AppInsegura.Datos;
using AppInsegura.Modelos;
using AppInsegura.Servicios;

namespace AppInsegura
{
    public class Program
    {
        private static readonly BaseDatosUsuarios baseDatos = new BaseDatosUsuarios();
        private static readonly AuthService auth = new AuthService(baseDatos);
        private static Usuario? usuarioActual = null;

        public static void Main(string[] args)
        {
            CargarUsuariosDeEjemplo();

            Console.WriteLine("=== Gestor de Usuarios y Partidas ===");
            // ARREGLO: quitada la línea que mostraba "admin/admin1234, ana/ana2024".
            // Motivo: enseñaba las contraseñas (incluida la del admin) a cualquiera que abriera la app.
            Console.WriteLine();

            bool salir = false;
            while (!salir)
            {
                MostrarMenu();
                string opcion = Console.ReadLine() ?? "";

                try
                {
                    switch (opcion)
                    {
                        case "1":
                            Registrar();
                            break;
                        case "2":
                            IniciarSesion();
                            break;
                        case "3":
                            BuscarUsuario();
                            break;
                        case "4":
                            VerPerfil();
                            break;
                        case "5":
                            PanelAdministracion();
                            break;
                        case "6":
                            SincronizarConServidor();
                            break;
                        case "0":
                            salir = true;
                            break;
                        default:
                            Console.WriteLine("Opción no válida.");
                            break;
                    }
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                // ARREGLO: los errores de validación (contraseña corta, usuario repetido...) muestran solo nuestro mensaje.
                // Motivo: el usuario sabe qué ha hecho mal sin ver detalles internos.
                catch (Exception)
                {
                    Console.WriteLine("Ha ocurrido un error inesperado.");
                }
                // ARREGLO: quitado Console.WriteLine(ex.ToString()).
                // Motivo: mostraba la traza completa del error (clases, rutas, líneas), información útil para un atacante.

                Console.WriteLine();
            }

            Console.WriteLine("Hasta luego.");
        }

        private static void CargarUsuariosDeEjemplo()
        {
            string? claveAdmin = Environment.GetEnvironmentVariable("ADMIN_PASSWORD");
            string? claveAna = Environment.GetEnvironmentVariable("ANA_PASSWORD");

            if (claveAdmin != null) auth.Registrar("admin", claveAdmin, "admin");
            if (claveAna != null) auth.Registrar("ana", claveAna, "jugador");
        }
        // ARREGLO: las contraseñas ya no están escritas en el código; se leen de las variables de entorno
        // ADMIN_PASSWORD y ANA_PASSWORD (si no existen, ese usuario no se crea).
        // Motivo: con "admin1234" en el código, cualquiera que lo vea (por ejemplo en GitHub) entra como admin.

        private static void MostrarMenu()
        {
            Console.WriteLine("------------------------------------");
            Console.WriteLine($"Usuario actual: {(usuarioActual != null ? usuarioActual.Nombre : "ninguno")}");
            Console.WriteLine("1. Registrar usuario");
            Console.WriteLine("2. Iniciar sesión");
            Console.WriteLine("3. Buscar usuario por nombre");
            Console.WriteLine("4. Ver mi perfil");
            if (usuarioActual != null && usuarioActual.Rol == "admin")
            {
                Console.WriteLine("5. Panel de administración");
            }
            Console.WriteLine("6. Sincronizar partida con el servidor");
            Console.WriteLine("0. Salir");
            Console.Write("Elige una opción: ");
        }

        private static void Registrar()
        {
            Console.Write("Nombre de usuario: ");
            string nombre = Console.ReadLine() ?? "";
            Console.Write("Contraseña: ");
            string contrasena = Console.ReadLine() ?? "";

            Usuario nuevo = auth.Registrar(nombre, contrasena);
            Console.WriteLine($"Usuario '{nuevo.Nombre}' registrado con rol '{nuevo.Rol}'.");
        }

        private static void IniciarSesion()
        {
            Console.Write("Nombre de usuario: ");
            string nombre = Console.ReadLine() ?? "";
            Console.Write("Contraseña: ");
            string contrasena = Console.ReadLine() ?? "";

            Usuario? usuario = auth.IniciarSesion(nombre, contrasena);
            if (usuario == null)
            {
                Console.WriteLine("Usuario o contraseña incorrectos.");
                return;
            }

            usuarioActual = usuario;
            Console.WriteLine($"Bienvenido, {usuario.Nombre}.");
        }

        private static void BuscarUsuario()
        {
            Console.Write("Nombre a buscar: ");
            string nombre = Console.ReadLine() ?? "";

            Usuario? encontrado = baseDatos.BuscarPorNombre(nombre);
            Console.WriteLine(encontrado != null
                ? $"Encontrado: {encontrado.Nombre} (rol: {encontrado.Rol})"
                : "No se ha encontrado ningún usuario con ese nombre.");
        }

        private static void VerPerfil()
        {
            if (usuarioActual == null)
            {
                Console.WriteLine("Primero debes iniciar sesión.");
                return;
            }

            Console.WriteLine($"Nombre: {usuarioActual.Nombre}");
            Console.WriteLine($"Rol: {usuarioActual.Rol}");
            // ARREGLO: quitada la línea que mostraba el token de sesión.
            // Motivo: el token es secreto; si alguien lo ve, puede hacerse pasar por ti.
        }

        private static void PanelAdministracion()
        {
            if (usuarioActual == null || usuarioActual.Rol != "admin")
            {
                Console.WriteLine("Opción no válida.");
                return;
            }
            // ARREGLO: se comprueba que el usuario sea admin antes de entrar al panel.
            // Motivo: antes la opción 5 solo se ocultaba en el menú, pero escribiendo "5" entraba cualquiera, incluso sin iniciar sesión.

            Console.WriteLine("=== PANEL DE ADMINISTRACIÓN ===");
            Console.WriteLine("Lista de usuarios registrados:");
            foreach (Usuario u in baseDatos.ListarTodos())
            {
                Console.WriteLine($" - {u.Nombre} ({u.Rol})");
            }
        }

        private static void SincronizarConServidor()
        {
            if (usuarioActual == null)
            {
                Console.WriteLine("Primero debes iniciar sesión.");
                return;
            }

            var red = new RedService();
            red.EnviarPuntuacion(usuarioActual.Nombre, 1000);
        }
    }
}