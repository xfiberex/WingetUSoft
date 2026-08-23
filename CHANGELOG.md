# Registro de cambios

Este archivo **no duplica** la lista de versiones: la señala. Hay dos registros y los dos se escriben
solos como parte del flujo de trabajo, así que una tercera copia a mano solo serviría para quedarse atrás.

## Qué cambió en cada versión

👉 **[Releases](https://github.com/xfiberex/WingetUSoft/releases)** — una entrada por versión, escrita
para quien usa la app: qué se arregló y qué cambia para ti. Es lo mismo que la aplicación te enseña en el
diálogo **Novedades** después de actualizar.

Cada release publica dos archivos: el instalador y su `.sha256`. Cómo verificarlo, en
[SECURITY.md](SECURITY.md).

## Por qué cambió

👉 **[CONTEXT.md § Registro de cambios](CONTEXT.md#registro-de-cambios)** — la tabla completa de versiones
con fecha, y debajo una sección por corte con el **razonamiento**: qué problema había, qué alternativas se
descartaron y por qué. Es el registro para quien toca el código.

👉 **[ROADMAP.md § Progreso](ROADMAP.md#-progreso)** — el registro por tarea. Cada una con su ID, el commit
que la cerró y la versión en que salió, de forma que se puede ir de una línea del plan al cambio concreto.

## Comparar dos versiones

Los tags siguen el formato `vX.Y.Z`, así que el diff exacto entre dos versiones sale de una URL:

```
https://github.com/xfiberex/WingetUSoft/compare/v1.8.6...v1.8.7
```

## Versionado

[SemVer](https://semver.org/lang/es/) sobre una fuente única: `<Version>` en
`src/WingetUSoft/WingetUSoft.csproj`. `release.ps1` la sube junto con `<AssemblyVersion>` y
`<FileVersion>` en el mismo paso, y la app y el actualizador leen `AssemblyVersion` — no hay una segunda
copia del número que pueda desincronizarse.
