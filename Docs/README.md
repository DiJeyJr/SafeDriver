# Documentación de SafeDriver

Guías para extender el juego sin tocar el código core.

## Índice

- **🎯 [Guía del Level Designer](GuiaLevelDesigner.md)** — **EMPEZÁ POR ACÁ.** Armar
  un nivel completo de cero en 6 pasos, usando los **prefabs de zona drag-and-drop**
  (semáforo, PARE, senda, etc.). Es la guía principal y autocontenida.
- **[Cómo crear misiones](CrearMisiones.md)** — detalle fino de los tipos de misión
  (contables, secuencia, con tiempo, compuestas).
- **[Cómo crear niveles](CrearNiveles.md)** — teoría de escenas/progresión y modo
  libre (complemento de la guía principal). *Nota: la parte de "colocar detectores a
  mano" quedó vieja — usá los prefabs de la Guía del Level Designer.*

## Sobre las imágenes

Las guías tienen marcadores `📷 _(screenshot: ...)_` donde conviene una captura.
Esas capturas son del **editor de Unity** (Inspector, Project, Build Settings),
que se sacan a mano. Para completarlas:

1. Sacá el screenshot desde Unity (Win+Shift+S o la herramienta que uses).
2. Guardalo en `Docs/img/` con un nombre descriptivo (ej. `mision-contable-inspector.png`).
3. En el `.md`, reemplazá la línea del marcador por: `![descripción](img/nombre.png)`.

### Lista de capturas pendientes

**CrearMisiones.md**
- [ ] `mision-contable-inspector.png` — Inspector de una CountableMissionDefinition completa
- [ ] `mision-secuencia-steps.png` — array Steps con varios ActionType en orden
- [ ] `mision-timed.png` — TimedMissionDefinition con Time Limit Seconds
- [ ] `mision-compuesta.png` — CompoundMissionDefinition con sub-misiones

**CrearNiveles.md**
- [ ] `leveldefinition-inspector.png` — LevelDefinition con misiones asignadas
- [ ] `escena-jerarquia.png` — jerarquía mostrando los detectores en la escena
- [ ] `missionsystem-componentes.png` — GameObject MissionSystem con MissionManager + LevelManager

> Las guías son útiles sin las imágenes (el paso a paso es completo); las
> capturas son un complemento visual.
