namespace AppInsegura.Modelos
{
    public static class Roles
    {
        public const string Admin = "admin";
        public const string Jugador = "jugador";
    }
    // ARREGLO: se crean constantes para los roles en lugar de escribir "admin" / "jugador" a mano por todo el código.
    // MOTIVO: evita errores de escritura en comprobaciones de seguridad (un "Admin" mal escrito podría saltarse un control de acceso).

    public class Usuario
    {
        public string Nombre { get; init; } = "";
        // ARREGLO: el nombre pasa de "set" a "init" (solo se puede asignar al crear el usuario).
        // MOTIVO: impide que otra parte del código cambie el nombre de un usuario ya creado y suplante a otro.

        public string ContrasenaHash { get; set; } = "";

        public string Sal { get; set; } = "";
        // ARREGLO: nuevo campo "Sal" (salt) aleatorio y distinto para cada usuario.
        // MOTIVO: sin sal, dos usuarios con la misma contraseña tienen el mismo hash y se pueden romper en bloque con tablas arcoíris.

        public string Rol { get; init; } = Roles.Jugador;
        // ARREGLO: el rol pasa de "set" a "init" y usa la constante Roles.Jugador.
        // MOTIVO: el rol no se puede modificar después de crear el usuario, así nadie puede subirse a admin cambiando la propiedad.

        public string TokenSesion { get; set; } = "";

        public int IntentosFallidos { get; set; } = 0;
        public DateTime? BloqueadoHasta { get; set; } = null;
        // ARREGLO: nuevos campos para contar intentos de login fallidos y bloquear la cuenta temporalmente.
        // MOTIVO: antes se podían probar contraseñas sin límite (ataque de fuerza bruta / diccionario).
    }
}