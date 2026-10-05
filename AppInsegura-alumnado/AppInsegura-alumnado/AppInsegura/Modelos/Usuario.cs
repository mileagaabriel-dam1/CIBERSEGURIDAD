namespace AppInsegura.Modelos
{
    public class Usuario
    {
        public string Nombre { get; set; } = "";
        public string ContrasenaHash { get; set; } = "";
        public string Rol { get; set; } = "jugador";
        public string TokenSesion { get; set; } = "";
    }
}
