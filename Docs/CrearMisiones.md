# Cómo crear misiones

Esta guía explica cómo armar misiones para SafeDriver usando el sistema
data-driven de `SafeDriver.Missions`. No hace falta tocar código: las misiones
son **assets** (ScriptableObjects) que se crean y configuran desde el Inspector.

---

## Concepto

Una misión tiene dos partes:

- **Definition** — el *dato*: un asset que vos creás y configurás (qué hay que
  hacer, cuántas veces, cuántos puntos da). Es lo único que tocás.
- **Runtime** — la *lógica*: una clase de C# que el `MissionManager` crea sola a
  partir de la Definition cuando arranca el nivel. No la tocás.

El flujo en juego es:

```
Detector (semáforo, peatón, etc.)
   └─ dispara una acción correcta vía EventBus  (ej. StoppedAtRedLight)
        └─ MissionRuntime escucha y avanza su progreso
             └─ MissionManager avisa a la UI (panel de objetivos del tablero)
```

Las misiones **no** referencian a los detectores directamente: se conectan por
el tipo de acción (`ActionType`). Si un detector dispara `StoppedAtRedLight` y
hay una misión que cuenta `StoppedAtRedLight`, avanza. Así de simple.

---

## Tipos de misión

| Tipo | Cuándo usarlo | Menú de creación |
|------|---------------|------------------|
| **Contable** | "Hacé X acción N veces" (lo más común) | `SafeDriver/Misiones/Contable` |
| **Secuencia** | "Hacé A, después B, después C, en orden" | `SafeDriver/Misiones/Secuencia` |
| **Con tiempo** | "Hacé X antes de que se acabe el tiempo" | `SafeDriver/Misiones/Con tiempo` |
| **Compuesta** | "Completá estas sub-misiones (y no falles ninguna)" | `SafeDriver/Misiones/Compuesta` |

---

## Acciones disponibles (`ActionType`)

Estas son las acciones que los detectores ya disparan. Tu misión "Contable" o
los pasos de una "Secuencia" se enganchan a una de estas:

| ActionType | Qué la dispara | Detector |
|------------|----------------|----------|
| `StoppedAtRedLight` | Frenar y quedarse detenido en semáforo rojo | TrafficLightDetector |
| `PassedGreenLight` | Cruzar el semáforo en verde | TrafficLightDetector |
| `StoppedAtPareSign` | Detención completa en señal PARE | StopSignDetector |
| `YieldedToPedestrian` | Ceder el paso a un peatón cruzando | PedestrianCrossingDetector |
| `PedestrianNotPresent` | Cruzar la senda cuando no hay peatones | PedestrianCrossingDetector |
| `CheckedMirrorsBeforeTurn` | Mirar los espejos antes de girar | GazeMirrorDetector |
| `MaintainedLegalSpeed` | Mantener la velocidad dentro del límite | SpeedLimitZone |

> Si necesitás una acción nueva, hay que agregarla al enum `ActionType`
> (`Assets/_SafeDriver/Scripts/Core/ActionType.cs`) y hacer que algún detector
> la dispare. Eso sí es trabajo de código.

---

## Crear una misión Contable (paso a paso)

Es el tipo más usado. Ejemplo: "Detenerse en semáforo rojo, 1 vez".

1. En la ventana **Project**, andá a la carpeta donde querés guardarla
   (ej. `Assets/_SafeDriver/Missions/Level01City/`).
2. Click derecho → **Create → SafeDriver → Misiones → Contable**.
3. Nombrá el asset (ej. `m_rojo`).
4. Seleccionalo y completá los campos en el **Inspector**:

   | Campo | Qué poner | Ejemplo |
   |-------|-----------|---------|
   | Mission Id | Id único, sin espacios | `m_rojo` |
   | Title | Texto que ve el jugador en el panel | `Detenerse en semáforo rojo` |
   | Description | Explicación larga (opcional) | `Frená completamente ante la luz roja.` |
   | Points | Puntos al completar | `10` |
   | Is Optional | Si NO bloquea terminar el nivel | desmarcado |
   | Action | La acción que cuenta | `Stopped At Red Light` |
   | Required Count | Cuántas veces | `1` |

```
┌─ Inspector: m_rojo (CountableMissionDefinition) ─────┐
│ Mission Id     [ m_rojo                    ]         │
│ Title          [ Detenerse en semáforo rojo]         │
│ Description     ┌──────────────────────────┐         │
│                 │ Frená completamente...    │         │
│                 └──────────────────────────┘         │
│ Points         [ 10 ]                                │
│ Is Optional    [ ] (desmarcado = obligatoria)        │
│ ─ Contable ─                                         │
│ Action         [ Stopped At Red Light ▾ ]            │
│ Required Count [ 1 ]                                  │
└──────────────────────────────────────────────────────┘
```

> 📷 _(screenshot: Inspector de una CountableMissionDefinition completa)_

¡Listo! La misión ya existe. Para que aparezca en un nivel, agregala al
`LevelDefinition` (ver [CrearNiveles.md](CrearNiveles.md)).

---

## Crear una misión de Secuencia

Ejemplo: "Recorré el circuito: pasá el primer semáforo en verde, después
detenete en el PARE, después cedé al peatón".

1. **Create → SafeDriver → Misiones → Secuencia**.
2. Completá Mission Id / Title / Points como antes.
3. En **Steps**, expandí el array y poné las acciones **en el orden requerido**:
   - Element 0: `Passed Green Light`
   - Element 1: `Stopped At Pare Sign`
   - Element 2: `Yielded To Pedestrian`

La misión avanza solo si las acciones ocurren en ese orden. Una acción fuera de
orden se ignora (no avanza ni falla).

> 📷 _(screenshot: array Steps con 3 ActionType en orden)_

---

## Crear una misión Con tiempo

Ejemplo: "Detenete en 2 señales PARE en menos de 90 segundos".

1. **Create → SafeDriver → Misiones → Con tiempo**.
2. Campos extra:
   - **Action**: `Stopped At Pare Sign`
   - **Required Count**: `2`
   - **Time Limit Seconds**: `90`
   - **Start On Level Begin**: marcado = el reloj arranca al empezar el nivel;
     desmarcado = arranca cuando hacés la primera de las acciones.

Si el tiempo se agota antes de llegar al count, la misión queda **fallida**
(se muestra con una cruz roja en el panel).

> 📷 _(screenshot: TimedMissionDefinition con timeLimitSeconds)_

---

## Crear una misión Compuesta

Ejemplo: "Conducción segura en la rotonda" = ceder al peatón **y** respetar el
semáforo, sin cometer infracciones.

1. Primero creá las sub-misiones (Contables, por ejemplo).
2. **Create → SafeDriver → Misiones → Compuesta**.
3. En **Sub Missions**, arrastrá los assets de las sub-misiones.
4. **Fail On Any Sub Failure**: si está marcado, la compuesta falla apenas
   falla una sub-misión.

La compuesta se completa cuando **todas** sus sub-misiones se completan.

> 📷 _(screenshot: CompoundMissionDefinition con sub-misiones asignadas)_

---

## Probar una misión rápido (sin armar un nivel entero)

1. Abrí `Level_01_City` (ya tiene el sistema montado).
2. Seleccioná el GameObject **MissionSystem** → componente **LevelManager** →
   en su `Level` está el `LevelDefinition` `Level_01_City`.
3. Abrí ese `LevelDefinition` y agregá/quitá misiones del array **Missions**.
4. Play. El panel de objetivos del tablero refleja las misiones cargadas.

> Tip: para ver el progreso sin manejar, podés disparar acciones desde un script
> de prueba con `EventBus.Dispatch_CorrectAction(ActionType.StoppedAtRedLight, 10)`.

---

Siguiente: [Cómo crear niveles](CrearNiveles.md).
