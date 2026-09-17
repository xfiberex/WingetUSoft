# Contribuir a WingetUSoft

Proyecto de **autor único**. No busco mantenedores, pero los reportes y las propuestas se agradecen, y el
README invita a compilar desde el código — así que aquí está lo que hace falta saber para que eso salga
bien a la primera.

Si lo que has encontrado es una **vulnerabilidad**, no abras un issue: usa el canal privado descrito en
[SECURITY.md](SECURITY.md).

## Reportar un fallo

Un buen reporte cabe en tres líneas: qué hiciste, qué esperabas y qué pasó. Añade la versión
(*Ayuda → Acerca de*), la de Windows y el **idioma de la interfaz** — varios fallos históricos solo
aparecían con Windows en un idioma distinto del español, porque winget traduce su salida.

Si la app falló de forma fea, mira `%LocalAppData%\WingetUSoft\crash.log`. Si tienes el log de archivo
activado (Configuración), hay más contexto en `%LocalAppData%\WingetUSoft\logs\`.

## Proponer una idea

Mira antes el [ROADMAP.md](ROADMAP.md): puede que ya esté, con su razonamiento y su prioridad. Ahí también
verás lo que se **descartó** y por qué, que suele ser la parte más útil.

## Compilar y verificar

```powershell
dotnet build WingetUSoft.slnx
dotnet run --project src/WingetUSoft/WingetUSoft.csproj
```

Antes de proponer cualquier cambio, una sola orden decide si está verificado:

```powershell
.\verify.ps1          # compilación (warnings como error), estilo, unitarios, dependencias
.\verify.ps1 -Full    # además los UI tests de FlaUI, que conducen la app real
```

Los **UI tests** necesitan una sesión de escritorio interactiva y desatendida: no valen una sesión
bloqueada ni una consola sin escritorio. **No** necesitan elevación — la app corre `asInvoker`. Mientras
corren, no toques el ratón ni el teclado.

Para activar el hook que lanza la verificación rápida antes de cada `push`:

```powershell
git config core.hooksPath .githooks
```

### Qué verifica el CI y qué no

Cada push a `main` y cada pull request pasan por GitHub Actions (`.github/workflows/ci.yml`), que ejecuta
`verify.ps1` **sin** `-Full`: compilación sin advertencias, estilo, pruebas unitarias y dependencias
vulnerables. CodeQL analiza además el C# en busca de problemas de seguridad.

Lo que el CI **no** puede correr son los UI tests, que conducen la app real y necesitan un escritorio
interactivo. Si tu cambio toca la interfaz, ejecuta `.erify.ps1 -Full` en tu equipo y dilo en el pull
request: un CI en verde no basta para esos cambios.

## Convenciones que el repositorio comprueba

- **Cero advertencias.** `verify.ps1` compila con las advertencias tratadas como error.
- **Estilo según `.editorconfig`**, comprobado con `dotnet format` en las categorías `style` y
  `analyzers`. **No** se comprueba `whitespace`: el repositorio alinea en columnas a propósito
  (constantes, campos de interop, el diccionario de traducciones) y esa categoría querría deshacerlo.
- **Comentarios en español**, con acentos reales. El código —identificadores, tipos, API— en inglés.
- **Nada de cadenas de interfaz cableadas.** Todo texto que ve el usuario pasa por `L.T("clave")`, con sus
  cinco traducciones (ES/EN/PT/FR/IT). Hay tests que fallan si añades una clave sin traducir, si usas una
  clave inexistente, o si cableas un literal donde debería ir una clave.
- **Un cambio, sus tests.** Y que el test **discrimine**: si pasa igual con y sin la corrección, no está
  probando nada. La forma barata de comprobarlo es revertir el arreglo y ver que el test falla.

## Arquitectura, en una línea

`Core/` es lógica pura y testeable, sin UI ni efectos externos. `Services/` tiene los efectos (procesos,
red, disco). `Settings/` persiste. `UI/` es WinUI 3. `Localization/` son las cadenas. Si algo se puede
probar sin abrir una ventana, va en `Core/`.

El detalle completo, junto con las decisiones y sus porqués, está en [CONTEXT.md](CONTEXT.md).

## Commits

Mensaje en imperativo y, si el cambio cierra una tarea del roadmap, **su ID entre paréntesis**:

```
fix(cleanup): validar contención de rutas (T0-01)
```

Es lo que permite ir de una tarea del ROADMAP al cambio que la cerró. Cuando el cambio altere una decisión
o el estado del proyecto, actualiza [CONTEXT.md](CONTEXT.md) **en el mismo commit**: ese documento existe
para que el contexto viaje entre equipos, y sirve de poco si va por detrás del código.

## Licencia

Al contribuir aceptas que tu aportación se distribuya bajo la [MIT License](LICENSE), igual que el resto
del proyecto.
