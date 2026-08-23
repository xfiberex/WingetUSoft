# Política de seguridad

WingetUSoft descarga instaladores de Internet, los ejecuta y, cuando se lo pides, hace parte de ese
trabajo **con privilegios de administrador**. Además borra carpetas de forma recursiva al limpiar
residuos. Un fallo en cualquiera de esos caminos no es un fallo cosmético, así que este documento existe
para que puedas avisar por un canal privado antes de que sea público.

## Versiones con soporte

| Versión | Soporte |
|---|---|
| Última publicada (1.8.x) | ✅ |
| Cualquier versión anterior | ❌ |

Es un proyecto de autor único: no hay ramas de mantenimiento. Las correcciones salen en la siguiente
versión y la vía de actualización es la propia app (*Ayuda → Buscar actualización de WingetUSoft…*) o
el instalador más reciente de [Releases](https://github.com/xfiberex/WingetUSoft/releases).

## Cómo reportar una vulnerabilidad

**No abras un issue público.** Usa el canal privado de GitHub:

> **[Security → Report a vulnerability](https://github.com/xfiberex/WingetUSoft/security/advisories/new)**

Queda entre tú y el mantenedor hasta que haya corrección publicada.

Ayuda mucho incluir:

- versión de WingetUSoft (*Ayuda → Acerca de*) y de Windows;
- qué hiciste, paso a paso, y qué esperabas que pasara;
- si el fallo requiere elevación, interacción del usuario, o un paquete de winget concreto;
- lo que consideres impacto real: qué puede hacer alguien que lo aproveche.

Una prueba de concepto ayuda, pero no es imprescindible. Un reporte en prosa clara vale más que un
exploit que no se entiende.

### Qué esperar

Es un proyecto que se mantiene en ratos libres, así que no prometo tiempos de respuesta que no pueda
cumplir. Lo que sí:

- **Acuse de recibo** en cuanto lo lea.
- **Una respuesta con criterio**: si es un fallo, qué alcance le veo y por dónde iría la corrección; si
  creo que no lo es, el porqué — y si me equivoco, dímelo.
- **Crédito** en las notas del release, salvo que prefieras el anonimato.

Prefiero **divulgación coordinada**: dame margen para publicar la corrección antes de contarlo. Si el
fallo ya está siendo explotado o alguien más lo publicó, avísame y aceleramos.

## Dentro del alcance

Las zonas donde un fallo tiene consecuencias reales, con el archivo que las implementa:

- **Verificación de la auto-actualización** — `Services/GitHubUpdateService.cs`. Lo que permita ejecutar
  un instalador que **no** debería haber pasado la verificación.
- **Worker elevado** — `Services/WingetService.cs`. Es el que corre como administrador; se comunica por
  named pipe con `PipeOptions.CurrentUserOnly` y un token de autenticación. Lo que permita a otro proceso
  hablar con él, suplantarlo o colar comandos.
- **Limpieza de residuos** — `Services/CleanupScanner.cs`. Borra carpetas **recursivamente y sin pasar
  por la papelera**. Lo que consiga que proponga una ruta fuera de los directorios que examina.
- **Persistencia** — `Settings/AppSettings.cs`. Lo que permita que un `settings.json` manipulado provoque
  ejecución de código o escrituras fuera de `%LocalAppData%\WingetUSoft`.
- **Instalador** — `src/WingetUSoft/installer/`. Secuestro de DLL o EXE, permisos de directorio,
  cualquier cosa que aproveche que se instala con elevación.

## Fuera del alcance

No por desinterés, sino porque ya están decididos y documentados:

- **El instalador no está firmado.** No hay certificado de código, así que SmartScreen dice «editor
  desconocido». Es una limitación **conocida y asumida**, no un hallazgo. Está en el README y en las notas
  de cada release.
- **El modelo de confianza del `.sha256` tiene un techo declarado.** El hash y el `.exe` salen del mismo
  release, así que la verificación detecta manipulación o corrupción **en tránsito**, pero no protegería
  frente a un compromiso de la cuenta de GitHub del proyecto. Para eso hace falta firma Authenticode. Que
  esto sea así no es un fallo: es el alcance que el README declara. Lo que **sí** es un fallo es cualquier
  cosa que rompa la verificación dentro de ese alcance.
- **Vulnerabilidades de winget, del Windows App SDK o de .NET.** Repórtalas a sus proyectos. Si WingetUSoft
  las agrava por cómo los usa, eso sí es de aquí.
- **Los paquetes que instala winget.** WingetUSoft es una interfaz: no audita el software de terceros que
  el catálogo entrega.

## Cómo verificar una descarga

Cada release publica dos assets: el instalador y su `.sha256`. Para comprobarlo a mano:

```powershell
Get-FileHash .\WingetUSoft-Setup-x.y.z.exe -Algorithm SHA256
Get-Content .\WingetUSoft-Setup-x.y.z.exe.sha256
```

Los dos valores deben coincidir. La app hace esta misma comprobación sola antes de ejecutar cualquier
instalador que descargue, y si no cuadra **borra el archivo y aborta la actualización**.

## Privacidad

La aplicación no recopila datos personales ni telemetría. Se conecta a Internet únicamente para
consultar o instalar paquetes vía winget y para comprobar actualizaciones en GitHub Releases, siempre
por HTTPS.
