using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
            string consulta = $"SELECT * FROM usuarios WHERE nombre = '{nombreBuscado}'";
            return EjecutarConsultaSimulada(consulta);
        }

        // Simulación simplificada de un motor de consultas, únicamente para
        // que el ejercicio se pueda ejecutar sin una base de datos real.
        // Interpreta la cadena "consulta" igual que lo haría un motor SQL básico.
        private Usuario? EjecutarConsultaSimulada(string consulta)
        {
            Console.WriteLine($"[DB] {consulta}");

            if (consulta.Contains("' OR '1'='1") || consulta.Contains("' OR 1=1") || consulta.Contains("'='"))
            {
                return usuarios.FirstOrDefault();
            }

            Match coincidencia = Regex.Match(consulta, "nombre = '([^']*)'");
            if (!coincidencia.Success)
            {
                return null;
            }

            string nombre = coincidencia.Groups[1].Value;
            return usuarios.FirstOrDefault(u => u.Nombre == nombre);
        }
    }
}
