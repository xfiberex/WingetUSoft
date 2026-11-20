---
target: proyecto completo (src/WingetUSoft/UI)
total_score: 27
max_score: 40
na_heuristics: 
p0_count: 0
p1_count: 2
target_identity: "file:C:\\Users\\User\\Desktop\\Cursos y Proyectos\\11 - NET 10\\WingetUSoft\\src\\WingetUSoft\\UI"
timestamp: 2026-11-20T22-28-44Z
slug: src-wingetusoft-ui
---
Method: design review only (detector not applicable, native platform: WinUI 3 / XAML)

## Design Health Score
| # | Heuristic | Score | Key Issue |
|---|---|---|---|
| 1 | Visibility of System Status | 3 | Status bar, per-row status, taskbar progress. Gap: count duplicated in Uninstall |
| 2 | Match System / Real World | 3 | Release notes shown raw with hard wraps; "Fuente/Id" assume winget knowledge |
| 3 | User Control and Freedom | 3 | Cancel + Esc, skip version, cancel-by-default on destructive |
| 4 | Consistency and Standards | 2 | System accent (red here) indistinguishable from danger red; 3 selection models across 3 tables |
| 5 | Error Prevention | 3 | Good confirmations; broadest action ("Actualizar todo") is the visual primary |
| 6 | Recognition Rather Than Recall | 2 | Icon-only "Excluidos" column header; per-row actions buried in Herramientas (7 items) / context menu |
| 7 | Flexibility and Efficiency | 3 | F5, Ctrl+F, Ctrl+A, Supr, SplitButton |
| 8 | Aesthetic and Minimalist Design | 3 | Clean Fluent; permanent hint strip, duplicated empty messages |
| 9 | Error Recovery | 3 | Classified failure reasons + single summary |
| 10 | Help and Documentation | 2 | Settings descriptions good; no contextual help; Novedades badly rendered |
| **Total** | | **27/40** | **Acceptable (top of band)** |

## Priority Issues
1. [P1] System accent collides with danger red (Actualizar todo / Guardar / Cerrar look like Desinstalar on red-accent systems; F-05 intent fails). Fix: danger not by fill alone, or detect hue proximity; reconsider primary.
2. [P1] Novedades renders release notes with mid-sentence hard wraps, stray indentation and duplicated version line. Fix: reflow paragraphs in ReleaseNotes, drop redundant title line.
3. [P2] Historial empty: two messages for one state; search/filter/Exportar CSV enabled with nothing. Fix: one message, disable controls.
4. [P2] Main: icon-only exclusion column; per-row actions (omit version, exclude, details) hidden; permanent hint strip.
5. [P2] Cross-window inconsistency: checkbox multi-select vs single-select tables; "Actualizar lista" vs "Consultar"; duplicated counts.

## Persona Red Flags
- Alex: fine on keyboard; but "Actualizar todo" as primary invites the one-click mass update; single-select in Uninstall forces one-by-one.
- Jordan: can't find "Omitir esta versión"; icon-only column; Novedades text looks broken.
- Sam: icon-only header relies on tooltip; red accent vs danger = color-only meaning (glyph helps only on Desinstalar).

## Minor
- Combo text centered in Excluidos/Estado combos.
- Uninstall "Fuente" column mostly empty; Search "Estado" column empty until install.
- Settings uses explicit Guardar/Cancelar vs Windows 11 instant-apply.

## Questions
- Should the primary action follow the selection?
- Does Settings need a Save button?
