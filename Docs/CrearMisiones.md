# Cómo crear misiones

Esta guía explica cómo armar misiones para SafeDriver. Hay dos caminos:

- **Mission Kits (recomendado)** — prefabs drag-and-drop: 1 kit = 1 misión
  completa y funcional. No tocás assets ni código.
- **Manual (avanzado)** — crear los assets de misión a mano y cablearlos al
  nivel. Para casos que los kits no cubren (secuencias, tiempo, compuestas).

---

## La forma fácil: Mission Kits

En `Assets/_SafeDriver/Prefabs/MissionKits/` hay un prefab por misión. Cada kit
trae **todo** lo que la misión necesita: zona de detección, visuales, NPCs con
waypoints si corresponde, y la misión misma que se registra sola al arrancar la
escena.

**Uso: arrastrá el kit a la escena, apoyalo sobre la calle con la flecha azul
(+Z local) apuntando en el sentido de circulación. Listo.**

| Kit | Qué trae | Misión |
|-----|----------|--------|
| `MissionKit_Semaforo` | Semáforo funcionando (ciclo rojo/amarillo/verde) + zona de frenado + línea de cruce + zona de freno para autos NPC | Detenerse en el semáforo en rojo |
| `MissionKit_Pare` | Señal de PARE + zona de detención | Detenerse en la señal de PARE |
| `MissionKit_Velocidad` | Señal de límite + zona de 40 km/h (editable en el `SpeedLimitZone` del hijo `Zone`) | Respetar el límite de velocidad |
| `MissionKit_Peaton` | Senda peatonal pintada + zona de control + **peatón que camina** ida y vuelta cruzando la cebra + hitbox de atropello (infracción grave → SafeFail) | Ceder el paso al peatón |
| `MissionKit_Espejos` | Solo la misión (la detección la hace el auto con eye-gaze) — se puede dejar en cualquier lado | Chequear los espejos antes de girar (×2) |

### Ajustes por instancia (Inspector del kit)

Seleccioná el kit en la escena → componente **MissionKit**:

| Campo | Qué hace |
|-------|----------|
| Mission Template | La misión base del kit (no tocar salvo que sepas lo que hacés) |
| Custom Title | Si lo completás, reemplaza el título en el panel de objetivos |
| Required Count Override | Cuántas veces exige la acción. `0` = usa el valor del template |
| Is Optional | Marcada = la misión no bloquea la finalización del nivel |

### El kit del peatón

Los 4 waypoints del recorrido son hijos de `Waypoints` dentro del kit:
`WP_0_VeredaA → WP_1_Cebra → WP_2_Cebra → WP_3_VeredaB` (camina ida y vuelta).
Movelos para adaptar el cruce al ancho de tu calle. Los dos del medio son los
que cuentan como "peatón sobre la senda" — mantenelos dentro de la cebra.

### Cosas a saber

1. **Las misiones cuentan acciones globales** (EventBus): dos kits de PARE con
   count 1 se completan los dos con la primera parada. Para exigir 2 paradas
   usá **un** kit con Required Count Override = 2.
2. **El kit necesita el stack de gameplay en la escena** (GameManager,
   MissionManager, ScoreManager — `LevelDesign_Level` y `Level_01_City` ya lo
   tienen). Si falta, el kit avisa por consola y no registra nada.
3. **Funciona en niveles free-roam**: aunque el `LevelDefinition` del nivel esté
   en `isFreeRoam`, las misiones de los kits se registran igual — completarlas
   termina el nivel y desbloquea el siguiente.
4. **Regenerar los kits**: menú `SafeDriver → Prefabs → Crear Mission Kits
   (1 kit = 1 mision)`. Los templates de misión viven en
   `Assets/_SafeDriver/Missions/Templates/` y NO se pisan al regenerar (los
   ajustes que hagas ahí quedan).

> 📷 _(screenshot: MissionKit_Peaton en escena con sus waypoints visibles)_

---

## Concepto (cómo funciona por debajo)

Una misión tiene dos partes:

- **Definition** — el *dato*: un asset (qué hay que hacer, cuántas veces,
  cuántos puntos da).
- **Runtime** — la *lógica*: la clase que el `MissionManager` crea solo a
  partir de la Definition cuando arranca el nivel. No se toca.

```
Detector (semáforo, peatón, etc.)
   └─ dispara una acción correcta vía EventBus  (ej. StoppedAtRedLight)
        └─ MissionRuntime escucha y avanza su progreso
             └─ MissionManager avisa a la UI (panel de objetivos del tablero)
```

Las misiones **no** referencian a los detectores directamente: se conectan por
el tipo de acción (`ActionType`). Si un detector dispara `StoppedAtRedLight` y
hay una misión que cuenta `StoppedAtRedLight`, avanza.

Las misiones llegan al `MissionManager` por tres vías (compatibles entre sí):

1. **Mission Kits** — cada kit registra la suya al arrancar la escena.
2. **LevelDefinition** — el `LevelManager` inyecta el array `Missions` del
   asset del nivel (ver [CrearNiveles.md](CrearNiveles.md)).
3. **Inspector del MissionManager** — solo para escenas de prueba sin
   LevelManager (`autoLoadInspectorMissions`).

---

## Acciones disponibles (`ActionType`)

| ActionType | Qué la dispara | Detector |
|------------|----------------|----------|
| `StoppedAtRedLight` | Frenar y quedarse detenido en semáforo rojo | RedLightStopZone |
| `PassedGreenLight` | Cruzar el semáforo en verde | TrafficLightCrossLine |
| `StoppedAtPareSign` | Detención completa en señal PARE | StopSignDetector |
| `YieldedToPedestrian` | Ceder el paso a un peatón cruzando | PedestrianCrossingDetector |
| `PedestrianNotPresent` | Cruzar la senda cuando no hay peatones | PedestrianCrossingDetector |
| `CheckedMirrorsBeforeTurn` | Mirar los espejos antes de girar | GazeMirrorDetector |
| `MaintainedLegalSpeed` | Mantener la velocidad dentro del límite | SpeedLimitZone |

> Si necesitás una acción nueva, hay que agregarla al enum `ActionType`
> (`Assets/_SafeDriver/Scripts/Core/ActionType.cs`) y hacer que algún detector
> la dispare. Eso sí es trabajo de código.

---

## Manual (avanzado): crear assets de misión a mano

Para los tipos que los kits no cubren, o para armar el array de un
`LevelDefinition` clásico.

### Tipos disponibles

| Tipo | Cuándo usarlo | Menú de creación |
|------|---------------|------------------|
| **Contable** | "Hacé X acción N veces" (lo más común) | `SafeDriver/Misiones/Contable` |
| **Secuencia** | "Hacé A, después B, después C, en orden" | `SafeDriver/Misiones/Secuencia` |
| **Con tiempo** | "Hacé X antes de que se acabe el tiempo" | `SafeDriver/Misiones/Con tiempo` |
| **Compuesta** | "Completá estas sub-misiones (y no falles ninguna)" | `SafeDriver/Misiones/Compuesta` |

### Contable (paso a paso)

1. En **Project**, andá a la carpeta destino (ej. `Assets/_SafeDriver/Missions/`).
2. Click derecho → **Create → SafeDriver → Misiones → Contable**.
3. Completá en el **Inspector**:

   | Campo | Qué poner | Ejemplo |
   |-------|-----------|---------|
   | Mission Id | Id único, sin espacios | `m_rojo` |
   | Title | Texto que ve el jugador | `Detenerse en semáforo rojo` |
   | Description | Explicación larga (opcional) | `Frená completamente ante la luz roja.` |
   | Points | Puntos al completar | `10` |
   | Is Optional | Si NO bloquea terminar el nivel | desmarcado |
   | Action | La acción que cuenta | `Stopped At Red Light` |
   | Required Count | Cuántas veces | `1` |

4. Agregala al array **Missions** del `LevelDefinition` del nivel — o, mejor,
   ponele el asset como **Mission Template** a un `MissionKit` en la escena y
   te ahorrás el paso.

### Secuencia

**Create → SafeDriver → Misiones → Secuencia**. En **Steps** poné las acciones
**en el orden requerido** (ej: `PassedGreenLight` → `StoppedAtPareSign` →
`YieldedToPedestrian`). Una acción fuera de orden se ignora (no avanza ni
falla).

### Con tiempo

**Create → SafeDriver → Misiones → Con tiempo**. Campos extra: **Action** +
**Required Count** + **Time Limit Seconds**. `Start On Level Begin` marcado =
el reloj arranca al empezar el nivel; desmarcado = arranca con la primera
acción. Si el tiempo se agota, la misión queda **fallida** (cruz roja en el
panel).

### Compuesta

Creá primero las sub-misiones, después **Create → SafeDriver → Misiones →
Compuesta** y arrastralas a **Sub Missions**. `Fail On Any Sub Failure` marcada
= la compuesta falla apenas falla una sub. Se completa cuando se completan
todas.

---

## Probar una misión rápido

1. Abrí una escena con gameplay (`Level_01_City` o `LevelDesign_Level`).
2. Arrastrá el kit (o agregá el asset al `LevelDefinition`).
3. Play. El panel de objetivos del tablero refleja las misiones cargadas.

> Tip: para ver el progreso sin manejar, podés disparar acciones desde un
> script de prueba con
> `EventBus.Dispatch_CorrectAction(ActionType.StoppedAtRedLight, 10)`.

---

Siguiente: [Cómo crear niveles](CrearNiveles.md).
