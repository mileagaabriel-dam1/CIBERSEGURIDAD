using System;
using System.Collections.Generic;
using System.Linq;
using AppInsegura.Modelos;
// ARREGLO: se elimina "using System.Text.RegularExpressions".
// MOTIVO: ya no se interpreta la consulta como texto con expresiones regulares (era la base de la inyección SQL).

namespace AppInsegura.Datos
{
    // Simula una tabla de base de datos (equivalente a una tabla SQLite/sqflite).
    // No usa un motor real para que el proyecto compile sin dependencias externas.
    public class BaseDatosUsuarios
    {
        private readonly List<Usuario> usuarios = new List<Usuario>();

        public void Agregar(Usuario usuario)
        {
            if (BuscarExacto(usuario.Nombre) != null)
            {
                throw new InvalidOperationException("Ya existe un usuario con ese nombre.");
            }
            // ARREGLO: se comprueba que no exista ya un usuario con el mismo nombre antes de añadirlo.
            // MOTIVO: antes se podía registrar otro "admin" (o "Admin") duplicado y crear confusión o suplantar a un usuario existente.

            usuarios.Add(usuario);
        }

        public IReadOnlyList<Usuario> ListarTodos()
        {
            return usuarios.AsReadOnly();
        }
        // ARREGLO: devuelve una lista de solo lectura (IReadOnlyList) en vez de la List<Usuario> interna.
        // MOTIVO: antes quien llamaba a este método podía añadir, borrar o reemplazar usuarios directamente sin pasar por ningún control.

        public Usuario? BuscarExacto(string nombre)
        {
            return usuarios.FirstOrDefault(u => string.Equals(u.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
        }
        // ARREGLO: la comparación de nombres ignora mayúsculas/minúsculas (OrdinalIgnoreCase).
        // MOTIVO: evita que "Admin" y "admin" se consideren usuarios distintos y se usen para engañar a otros usuarios.

        public Usuario? BuscarPorNombre(string nombreBuscado)
        {
            const string consulta = "SELECT * FROM usuarios WHERE nombre = @nombre";
            var parametros = new Dictionary<string, string>
            {
                ["@nombre"] = nombreBuscado
            };
            return EjecutarConsultaParametrizada(consulta, parametros);
        }
        // ARREGLO: la consulta ya no se construye concatenando el texto del usuario ($"... '{nombreBuscado}'").
        //          Ahora es una consulta parametrizada: la SQL es fija (@nombre) y el valor viaja aparte como parámetro.
        // MOTIVO: era una INYECCIÓN SQL. Escribiendo  ' OR '1'='1  se obtenía el primer usuario (el admin) sin conocer su nombre.
        //         Con parámetros, lo que escribe el usuario se trata siempre como un dato, nunca como código SQL.

        // Simulación simplificada de un motor de consultas parametrizadas, únicamente para
        // que el ejercicio se pueda ejecutar sin una base de datos real.
        private Usuario? EjecutarConsultaParametrizada(string consulta, IReadOnlyDictionary<string, string> parametros)
        {
            if (!parametros.TryGetValue("@nombre", out string? nombre))
            {
                return null;
            }

            return usuarios.FirstOrDefault(u => string.Equals(u.Nombre, nombre, StringComparison.OrdinalIgnoreCase));
        }
        // ARREGLO: el valor del parámetro se compara literalmente con el nombre; ya no se buscan patrones como "' OR '1'='1" en el texto.
        // MOTIVO: el motor anterior "ejecutaba" lo que había dentro del texto del usuario, igual que un motor SQL vulnerable.

        // ARREGLO: se elimina la línea  Console.WriteLine($"[DB] {consulta}");
        // MOTIVO: mostraba por pantalla las consultas internas de la base de datos, dando pistas a un atacante de cómo inyectar SQL.
    }
}