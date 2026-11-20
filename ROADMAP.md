# WingetUSoft — Hoja de ruta

> **Qué hay aquí:** el estado de los tiers, las decisiones cerradas (y lo que se descartó a propósito) y
> lo que queda por verificar.
>
> **Lo ya completado** —las características de los Tiers A–E, el plan de auditoría T0–T4 y el Tier F, con
> sus tareas, registros y líneas base— vive en [`History.md`](History.md). El detalle de cómo se resolvió
> cada cosa y por qué, en [`CONTEXT.md`](CONTEXT.md) (§4 *Decisiones* y el *Registro de cambios*).
>
> **Propósito del proyecto:** GUI para **gestionar software con winget** — buscar e instalar, actualizar,
> desinstalar, y exportar/importar la lista de paquetes. *(Ampliado en el Tier E; hasta la v1.7.0 era solo
> "actualizaciones y desinstalaciones".)*

## Estado

| Tier | Tema | Estado | Versión |
|---|---|---|---|
| **A** | Paridad de infraestructura con FormatDiskPro | ✅ Completado | 1.3.0 |
| **B** | Layout adaptable, accesibilidad y UI tests | ✅ Completado | 1.4.0 / 1.4.1 |
| **C** | Auditoría de UI/UX (flujo, datos, color, accesibilidad) | ✅ Completado | 1.5.0 / 1.6.0 |
| **D** | Cara pública (licencia in-app, README, capturas) | ✅ Completado | 1.7.0 |
| **E** | Gestión completa de software ⚠️ *cambio de alcance* | ✅ Completado | 1.8.0 |
| **T0–T4** | Plan de acción de la auditoría técnica del 2026-08-20 (70 de 70) | ✅ Completado | 1.8.3 – 1.8.8 |
| **F** | Auditoría Fluent y UI/UX verificada en la app real (27 de 27) | ✅ Completado | 1.8.9 – 1.12.1 |

No hay ningún tier abierto. Lo próximo vendrá de uso real o de reportes; mientras, quedan las zonas sin
verificar del final de este archivo.

---

## 🚫 Decisiones cerradas (no reabrir)

- **Empaquetador:** Inno Setup es la única vía. Nada de MSIX/ClickOnce — el proyecto es y seguirá siendo
  unpackaged (`WindowsPackageType=None`).
- **Instalador framework-dependent** (no self-contained): descarga los runtimes que falten, verificado de
  punta a punta.
- **Licencia MIT** y **sin donaciones** (a diferencia de FormatDiskPro, GPLv3 + PayPal).
- **CI con GitHub Actions — reabierta y adoptada (2026-09-16), con alcance acotado.** Se descartó el
  2026-07-12 y se reafirmó el 2026-08-20 porque un runner hospedado no puede correr los UI tests y
  `release.ps1` ya cubría el corte. Con el repositorio público cambia la cuenta: los pull requests de
  terceros y de Dependabot necesitan una señal que no dependa del equipo del mantenedor. Lo que hay:
  - `.github/workflows/ci.yml`: el mismo `verify.ps1` sin `-Full` (build con `-warnaserror`, estilo,
    unitarios y dependencias vulnerables) en cada push a `main` y en cada PR. No duplica lógica: si
    cambia el script, cambia el workflow.
  - `.github/workflows/codeql.yml`: CodeQL para C# (`security-extended`), en push, en PR y cada semana. La
    primera ejecución, con `security-and-quality`, dio 374 alertas de calidad sin ninguna de seguridad (142
    en código generado); las 5 «error» de formato resultaron falsos positivos, ahora cubiertos por
    `LocalizationTests.EveryTranslation_UsesTheSamePlaceholdersAsSpanish`.
  - `.github/dependabot.yml`: NuGet y Actions, semanal; ignora las subidas mayores de WindowsAppSDK
    (T4-01 descartó la 2.x). Con NuGet apenas sirve: resuelve en Linux, da por incompatibles los paquetes
    para `net10.0-windows` y no abrió ningún PR pese a haber versiones nuevas. Por eso el CI ejecuta
    `verify.ps1` sin `-SkipOutdated` y deja el listado de desactualizados en su log.
  - Ajustes del repositorio: avisos y actualizaciones de seguridad de Dependabot, secret scanning con
    protección de push, y reporte privado de vulnerabilidades, el canal que ya indicaba `SECURITY.md`.

  **Lo que no cambia:** los UI tests siguen siendo solo locales (`verify.ps1 -Full`) y obligatorios antes
  de cada release, y `release.ps1` sigue cortando y publicando desde el equipo del mantenedor. Un CI en
  verde **no** da por verificada una tarea que toque la interfaz.
- **Certificado de firma de código (OV/EV) — descartado (2026-07-12):** consecuencia asumida — SmartScreen
  dirá "editor desconocido", y las actualizaciones se verifican por **SHA-256**. El soporte de firma sigue en
  el pipeline por si algún día hay certificado.
- **Fuera del propósito del producto:** gestión de discos (S.M.A.R.T. / benchmark / chkdsk — territorio de
  **FormatDiskPro**), presets (la lista de exclusiones cubre ese rol) y autorefresco por `WM_DEVICECHANGE`
  (específico de unidades extraíbles).
- **Notificación toast de Windows — descartada (2026-07-12):** era la cuarta idea del Tier E (avisar con un
  toast, con acción *Actualizar ahora*, cuando la auto-comprobación encuentra actualizaciones). **No se
  hará:** el aviso ya existe (sonido + parpadeo de la barra de tareas + progreso en el icono), y en una app
  **unpackaged** el toast exige registrar un servidor COM del `AppNotificationManager` — bastante fontanería
  de plataforma para un beneficio marginal sobre lo que ya se avisa.

---

# Pendiente de verificar

> Lo que las auditorías no pudieron cubrir. Los puntos tachados ya se resolvieron y se conservan como
> referencia; las tareas citadas (T0-01, F-24…) están en [`History.md`](History.md).

## Zonas que la auditoría no pudo cubrir

Pendientes de verificación con acceso a un entorno adecuado; no invalidan ningún hallazgo, pero acotan
hasta dónde llega la evidencia:

- ~~**UI tests (FlaUI):** no ejecutados~~ **resuelto el 2026-08-21.** Ejecutados en escritorio real:
  27/27 al cortar la 1.8.3 y **37/37** hoy. `release.ps1` los exige en cada corte.
- **Rendimiento medido:** sin *profiler*. Arranque, memoria y tiempo hasta la primera fila sin medir.
- ~~**Responsividad real (T2-11):**~~ **resuelto en la 1.8.6.** La fila de filtros envuelve, y la
  comprobación ya no sale de leer el XAML.
- ~~**Explotabilidad de T0-01:**~~ **resuelto el 2026-08-20.** Ya no es hipótesis: el test
  `ScanAsync_NameEscapesTheBaseDirectory_ProducesNoCandidate` demuestra que, sin la corrección, un
  paquete llamado `C:\Windows` hacía que el escáner ofreciera **`C:\Windows`** como residuo
  eliminable. Lo que sigue sin comprobarse es el eslabón previo: que un instalador real escriba un
  `DisplayName` así en Agregar o quitar programas.
- **Lector de pantalla real (T1-07, T1-08, T1-09):** *parcialmente cubierto.* Desde la 1.8.5 hay tests
  FlaUI que leen el árbol de automatización de la app real (`0c6aae7`), que es lo que consume un lector.
  Lo que sigue sin hacerse es escuchar una sesión con Narrador o NVDA.
- **Contraste fuera del registro:** solo se midieron `LogPalette` y los RGB cableados; el resto sale de
  `ThemeResource` de Windows, que se asume conforme. *(2026-09-14: medido en el Tier F — el botón de
  peligro de Desinstalar y Limpieza y las filas atenuadas **no** llegan a 4,5:1; ver F-05 y F-06.)*
- **Instalador end-to-end:** no ejecutado en una VM limpia sin .NET ni VC++ Redist.
- **Comportamiento en ARM64:** el instalador solo distribuye `win-x64` aunque el `.csproj` declare
  `win-arm64`; no verificado en hardware ARM.
- **Configuración del repositorio en GitHub:** *topics* y ramas protegidas no son accesibles desde el
  árbol de archivos. (Las alertas de seguridad del repositorio quedan fuera de alcance por decisión de
  proyecto: la vigilancia de dependencias es local, ver T2-13.)


## Zonas que el Tier F no pudo cubrir

- **Contraste alto:** no se activó, porque cambia la configuración del sistema del usuario (ver F-24).
- **Lector de pantalla real:** igual que en la Parte II, se leyó el árbol de UI Automation; no se escuchó
  una sesión con Narrador ni NVDA.
- **Limpieza de residuos:** no se condujo; solo se abre tras desinstalar un programa de verdad (el mismo
  límite que T1-09). Lo que le afecta en F-04 y F-05 sale del código.
- **Buscar e instalar:** no se condujo en esta pasada; lo que le afecta en F-07 y F-15 sale del código y de
  `docs/screenshots/search-*.png`.
- **Lotes de actualización reales:** hasta F-19 no se lanzó ninguno, y eso escondió que los lotes sin elevación
  estaban rotos desde la v1.8.8 (ver F-19). En F-19 se lanzó uno real, autorizado, de dos paquetes. El lote
  **elevado** sigue sin probarse de extremo a extremo.
