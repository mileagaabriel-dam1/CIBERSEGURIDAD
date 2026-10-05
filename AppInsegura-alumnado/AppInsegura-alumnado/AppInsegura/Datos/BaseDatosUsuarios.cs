using System;
using System.Collections.Generic;
using System.Linq;
using AppInsegura.Modelos;

namespace AppInsegura.Datos
{
    // Simula una tabla de base de datos (equivalente a una tabla SQLite/sqflite).
    // No usa un motor real para que el proyecto compile sin dependencias externas.
    public class BaseDatosUsuarios
    {
        private readonly List<Usuario> usuarios = new List<Usuario>();

        public void Agregar(Usuario usuario)
        {
            usuarios.Add(usuario);
        }

        public List<Usuario> ListarTodos()
        {
            return usuarios;
        }

        public Usuario? BuscarExacto(string nombre)
        {
            return usuarios.FirstOrDefault(u => u.Nombre == nombre);
        }

        public Usuario? BuscarPorNombre(string nombreBuscado)
        {
            return usuarios.FirstOrDefault(u => u.Nombre == nombreBuscado);
        }
        // ARREGLO: ya no se monta la consulta pegando el texto del usuario ("... WHERE nombre = '" + nombre + "'").
        // Ahora el nombre se compara directamente como un dato (igual que una consulta parametrizada).
        // Motivo: era una inyección SQL; escribiendo  ' OR '1'='1  se obtenía el usuario admin.

        // ARREGLO: eliminado el método EjecutarConsultaSimulada y su Console.WriteLine("[DB] ...").
        // Motivo: era el que "ejecutaba" la inyección y además mostraba las consultas internas por pantalla.
    }
}