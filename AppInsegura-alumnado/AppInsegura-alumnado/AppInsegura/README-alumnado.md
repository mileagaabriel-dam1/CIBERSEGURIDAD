# Gestor de Usuarios y Partidas

Esta aplicación de consola en C# simula un gestor de usuarios y partidas muy
sencillo: te puedes registrar, iniciar sesión, buscar usuarios, ver tu
perfil, entrar en un panel de administración (si tienes el rol adecuado) y
sincronizar tu puntuación con un servidor.

**Contiene fallos de seguridad deliberados.** Vuestro trabajo en las tres
sesiones es encontrarlos, entenderlos y corregirlos siguiendo los apuntes de
programación segura.

## Cómo ejecutarla

Necesitas el [SDK de .NET 8](https://dotnet.microsoft.com/download) instalado.

```bash
cd AppInsegura
dotnet run
```

Si usas Visual Studio o Visual Studio Code, basta con abrir la carpeta
`AppInsegura` y ejecutar el proyecto.

## Usuarios de prueba

Al arrancar, la aplicación crea automáticamente dos usuarios:

| Usuario | Contraseña | Rol |
|---|---|---|
| admin | admin1234 | admin |
| ana | ana2024 | jugador |

## Qué hacer en cada sesión

- **Sesión 1 (auditoría):** explorad el código y el comportamiento de la
  aplicación. Por cada fallo de seguridad que encontréis, rellenad una ficha
  con: dónde está, a qué mala práctica corresponde (podéis apoyaros en los
  apuntes de programación segura) y cómo lo aprovecharía un atacante. **No
  corrijáis nada todavía.**
- **Sesión 2 (corrección):** corregid el código aplicando las buenas
  prácticas que hemos visto en clase.
- **Sesión 3 (defensa cruzada):** intercambiaréis vuestro código corregido
  con otra pareja para poneros a prueba mutuamente.

## Una pista para empezar

No hace falta que solo leáis el código: probad también a **usar** la
aplicación como lo haría un usuario cualquiera, y fijaos en todo lo que se
muestra por pantalla mientras la usáis.
