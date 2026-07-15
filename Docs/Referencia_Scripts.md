# Referencia de Scripts — SafeDriver

Mapa rápido de todo el código del juego: qué hace cada script, cómo se conectan y
qué optimizaciones sostienen el framerate en Quest. Para armar niveles/misiones,
ver la [Guía del Level Designer](GuiaLevelDesigner.md) — esto es la referencia técnica.

---

## 1. Arquitectura en una pantalla

**Todo pasa por el EventBus.** Ningún sistema referencia a otro hacia arriba: el auto
publica eventos, y scoring/UI/audio/haptics reaccionan. Eso permite agregar features
sin tocar el código existente.

```
Vehicle ──publica──▶ EventBus ◀──publica── Scoring / Traffic
                        │
        escuchan: UI, Audio, Haptics, Missions, GameManager
```

- **`EventBus`** (Core) — eventos estáticos + un `Dispatch_*` por evento. Los importantes:
  - `OnSpeedChanged`, `OnSteeringChanged`, `OnGearChanged`, `OnHandbrakeChanged` (vehículo)
  - `OnInfractionDetected(tipo, msg)` / `OnCorrectActionPerformed(acción, bonus)` (scoring)
  - `OnScoreChanged`, `OnScoreDelta`, `OnLevelComplete`, `OnLevelFailed` (progresión)
  - `OnTrafficLightChanged`, `OnSpeedLimitChanged`, `OnGameStateChanged`
  - `Clear()` resetea todos los subscribers al cambiar de escena (evita callbacks a objetos muertos).
- **`GameManager`** (singleton) — máquina de estados del juego: `MainMenu → Driving → SafeFail / LevelEnd / Paused`.
  Controla `Time.timeScale` por estado. `TriggerSafeFail(tipo, msg)` setea la infracción
  y transiciona en el mismo call (evita el race donde la pantalla leía la infracción equivocada).
- **Asmdefs por carpeta** (`SafeDriver.Core/Vehicle/Scoring/Traffic/UI/VR/Audio/Missions`):
  las interfaces compartidas (ej. `IPedestrianCrossingNotifier`) viven en Core para
  romper dependencias circulares entre capas.

---

## 2. Vehicle — el auto

| Script | Qué hace |
|---|---|
| `VehicleController` | Núcleo: WheelColliders, motor/freno/dirección, máquina de marchas D/N/R. Publica velocidad y marcha al bus. `SmoothStop()` frena suave (~1.5s) al entrar en SafeFail — nada de frenadas bruscas en VR. |
| `VehiclePhysics` | Baja el centro de masa (anti-vuelco) y downforce opcional. No toca input. |
| `VehicleInput` | Lee gatillos XR (derecho=acelerar, izquierdo=frenar) vía Input System. El volante le inyecta el steering con `SetSteering()`. |
| `GearShifter` | Palanca de cambios VR (D/N/R por ángulo). Histéresis anti-flicker, snap al soltar, y se traba en su zona si el auto está en movimiento. |
| `HandbrakeController` | Palanca de freno de mano de 2 posiciones. Arriba=puesto (freno full + corta motor). |
| `TurnSignalController` + `TurnSignalStalk` | Guiños: la palanca (stalk) mapea su ángulo a Off/Left/Right y el controller parpadea los materiales de las luces. |

**Flujo de marchas:** sin palanca, D↔R se arma frenando 2 segundos (estados
`DriveArming`/`ReverseArming`, la `GearIndicator` parpadea). Con `GearShifter` en escena,
la palanca es la única autoridad y esa lógica se apaga.

---

## 3. VR — interacción y confort

| Script | Qué hace |
|---|---|
| `SteeringWheelController` | Volante agarrable (Meta ISDK). Sin Rigidbody propio — su collider pertenece al Rigidbody del auto, lo que eliminó el jitter de Rigidbodies anidados. Convierte la rotación en `VehicleInput.SetSteering()`. |
| `SteeringWheelHandFollower` / `GrabbableHandFollower` | Pegan la mano virtual al objeto agarrado (snapshot del pose + LateUpdate), en vez de que la mano flote donde está el tracking real. |
| `WheelGrabProximityFilter` / `GrabProximityFilter` | Solo se puede agarrar si la mano está realmente cerca del objeto (evita agarrar el volante desde el asiento de atrás). |
| `GrabLimitFeedback` | Vibración escalonada al forzar una palanca más allá de su límite; el último tier suelta el agarre y sacude la cámara. |
| `GrabHaptics` / `HapticsController` | Vibración de controles (API XR estándar). `HapticsController` reacciona solo al bus: infracción → patrón de error, acierto → patrón de éxito. |
| `MirrorCamera` | Espejos retrovisores — ver sección de optimizaciones. |
| `HeadTrackingDetector` / `GazeMirrorDetector` | Detectan que el jugador miró un espejo (por rotación de cabeza, o por raycast de mirada a colliders con tag "Mirror") y premian el chequeo. |
| `CameraShake` | Shake corto y chico (<5 cm) — más que eso marea en VR. |
| `DisplayRefreshRate` | Sube el visor de 72 a 90 Hz (persistido; el slider de opciones lo cambia en vivo). |
| `ControllerOnlyHands`, `HeadPoseFollower`, `HandGrabRigidbodyTracker`, `ClipboardHolster`, `XRDiagnostic` | Workarounds del SDK: apagar manos trackeadas duplicadas, fallback de pose de HMD, sincronizar el Rigidbody de la mano para que ISDK detecte overlaps, tablilla que vuelve sola a su lugar, y logger de diagnóstico XR. |

---

## 4. Scoring — puntaje e infracciones

**Flujo:** arrancás con **1000 puntos** (se aprueba con 600). Cada detector es un
trigger collider en la escena que, según lo que hagas, dispara
`Dispatch_Infraction` (resta) o `Dispatch_CorrectAction` (suma, tabla `ActionPoints`:
frenar en rojo +10, ceder al peatón +15, espejos +5, etc.). `ScoreManager` lleva la
cuenta, guarda el historial (`InfractionRecord`) y si la infracción es **grave**
(cruzar en rojo, no ceder al peatón, atropello, choque fuerte) llama
`GameManager.TriggerSafeFail` → el auto frena suave y aparece la pantalla pedagógica.
**Nunca se muestra el accidente.**

| Script | Qué detecta |
|---|---|
| `TrafficLightStopZone` + `TrafficLightCrossLine` | Semáforo: premio por frenar en rojo; infracción/premio al cruzar la línea según la luz. (Reemplazan al viejo `TrafficLightDetector`, obsoleto.) |
| `StopSignDetector` | Detención completa en el PARE. |
| `SpeedLimitZone` | Exceso de velocidad en la zona; premia mantener velocidad legal (publica el límite al HUD). |
| `PedestrianCrossingDetector` + `YieldZone` | Ceder el paso en la senda: infracción si pasás con peatones cruzando, premio si frenás y esperás. |
| `PedestrianHitbox` | Atropello → SafeFail directo. |
| `DirectionZone` | Circular en contramano (eje Z local de la zona = sentido legal). |
| `RedLightStopZone` | Zona de frenado previa a la línea (separada para que frenar bien no cuente como infracción). |
| `VehicleIntegrity` | Daño del auto en 8 zonas (carrocería + 4 ruedas, 0–100). Golpes fuertes → SafeFail por `SevereCollision`. Aditivo: solo escucha triggers, no toca la física. |
| `LevelTimer` | Cuenta regresiva del nivel; a 0 dispara `LevelFailed`. |
| `VehicleSensor` / `CrossingLineSensor` / `InfractionDetector` | Clases base de todos los detectores (filtran que sea el auto del player, detectan cruce de línea con dirección). |

---

## 5. Missions — objetivos por nivel

Sistema data-driven con **ScriptableObjects** (menú `Create > SafeDriver`):

- **`LevelDefinition`** — un nivel: qué escena carga, qué misiones tiene, cuál sigue.
- Tipos de misión (`MissionDefinition` + su runtime):
  - **Contable** — hacer una acción N veces ("frenar en rojo x2").
  - **Con tiempo** — contable + límite; se agota → falla.
  - **Secuencia** — acciones en orden estricto (ruta A→B→C); fuera de orden se ignora.
  - **Compuesta** — se completa cuando todas sus sub-misiones se completan.
- **`MissionManager`** escucha las acciones del EventBus y avanza las misiones; la UI
  (`ObjectivesController`) se redibuja con sus eventos `MissionsLoaded`/`MissionChanged`.
- **`LevelManager`** (corre antes, `DefaultExecutionOrder(-60)`) inyecta las misiones del
  `LevelDefinition` activo y marca el nivel completado en **`LevelProgress`** (PlayerPrefs:
  desbloqueo, mejor score, modo libre).
- **`MissionKit`** — prefab drag-and-drop que trae zona de detección + misión juntas.
  Ojo: las contables cuentan acciones *globales* — para exigir N paradas usar un kit con
  requiredCount N, no N kits.

Detalle fino en [CrearMisiones.md](CrearMisiones.md).

---

## 6. Traffic — NPCs

Los NPC **no usan física real ni NavMesh** (los principales): waypoints + Rigidbody
kinematic. Barato y determinista.

| Script | Qué hace |
|---|---|
| `TrafficWaypointPath` | Path de waypoints (children de un GO vacío), cerrado (loop) o abierto (rebota). |
| `TrafficVehicle` | Auto NPC: sigue el path con `MovePosition` en FixedUpdate + interpolación → velocidad consistente (no depende del framerate, clave en VR) y movimiento suave. Frena ante `TrafficStopLine` en rojo; si ya pasó la línea, termina de cruzar (no se planta en el medio). |
| `TrafficPedestrian` / `CrossingPedestrian` | Peatones por waypoints/lerp, sin NavMesh. Al cruzar la senda notifican al detector de scoring (`NotifyByInstance` — varios peatones no se pisan el estado). |
| `TrafficLightController` | Ciclo Verde→Amarillo→Rojo, swap de materiales, publica cada cambio al bus (NPCs y detectores reaccionan). |
| `TrafficLightStopZone` / `TrafficStopLine` | Dónde frenan: el player (trigger de scoring) y los NPC (por distancia a la línea). |
| `CrosswalkTrafficGate` | Frena a player y NPCs cuando hay peatones en la senda (chequeo por distancia, cachea referencias en Start). |
| `TrafficLayerIsolator` | Mete los NPC en la capa "Traffic" y desactiva su colisión contra todo lo demás por código — chocan con el player pero no se enganchan con calles/edificios. |
| `TrafficProfile` | ScriptableObject de dificultad: velocidad/densidad del tráfico por nivel sin tocar la escena. |
| `NPCVehicleAI` / `NPCPedestrianAI` / `DemoPedestrianFaker` | Versiones viejas (NavMesh / demo); las escenas nuevas usan los de arriba. |

---

## 7. UI — todo World Space en VR

Patrón de canvas: World Space + `PointableCanvas` + BB Ray Interaction + un EventSystem
con `PointableCanvasModule` (sin eso los botones no clickean). Escala 0.001. Ver CLAUDE.md.

| Script | Qué hace |
|---|---|
| `UIManager` | Orquestador: escucha `OnGameStateChanged` y muestra el panel que toca. No conoce Vehicle ni Scoring. |
| `HUDController` + `SpeedometerNeedle` + `GearIndicator` + `HandbrakeLight` + `VehicleDamageSymbol` | Tablero diegético: velocímetro de aguja, marcha D/N/R (parpadea en arming), testigo de freno de mano, símbolo de daño de 8 zonas. Todo alimentado por el bus. |
| `SafeFailScreen` | Pantalla pedagógica de infracción grave: qué hiciste mal + referencia a la Ley 24.449. Reintentar / Menú. |
| `LevelEndPanel` | Resumen de fin de nivel (score, infracciones, aprobado/reprobado) desde `ScoreManager.GetLevelResult()`. |
| `ObjectivesController` | Lista de objetivos del tablero, generada en runtime a partir de las misiones activas. |
| `SuccessToast` | Toast breve sobre el volante: verde en acierto, rojo en infracción leve (las graves ya tienen SafeFail). |
| `PauseMenuController` | Pausa (`timeScale=0`) y reposiciona el canvas frente a la cabeza al abrir. |
| `MainMenuController` + `LevelSelectPanel` | Menú principal y selección de niveles con desbloqueo progresivo (`LevelProgress`). |
| `FpsCounter` / `FpsToggleButton` / `RefreshRateSlider` | Contador de FPS flotante (auto-instanciado, coloreado según target) y opciones de refresco 72/80/90/120. |
| `Theme/UITheme` + `UIThemeUtil` | Tema visual central (un solo asset "EduTheme"): colores de señalética vial, tipografía legible en VR. Ninguna pantalla inventa sus colores. |
| `VRCanvasSetup` | Valida/configura los parámetros de canvas VR (font mínimo, distancia de confort 0.8–1.2 m). |

---

## 8. Audio

| Script | Qué hace |
|---|---|
| `AudioManager` | Crea sus AudioSources por código: motor 3D (pitch según velocidad, volumen según acelerador), ambience de ciudad 2D, SFX. Todo disparado por el bus (infracción, acierto, nivel completo). |
| `MasterVolumeController` | Volumen master vía AudioMixer, conversión lineal→dB (perceptual), persistido en PlayerPrefs. |
| `SpatialAudioEmitter` | Emisor 3D reutilizable para NPCs/semáforos (spatialBlend=1, HRTF lo pone Meta XR). |

---

## 9. Editor utilities (`Scripts/Editor/`)

~50 scripts **one-shot** que se corren desde el menú `SafeDriver/...` del editor. No
compilan al build. Existen porque `execute_code` de MCP falla en esta máquina, así que
todo setup pesado se hace con scripts persistentes. Grupos:

- **Setup de escena/nivel:** `Level01CitySetup`, `SetupCityTraffic`, `SetupDetectionV2`, `CreateMissionZonePrefabs`, `MissionSystemSetup`, `AddNpcStopZones`, `SetupCrossingPedestrians`…
- **Palancas VR (clonando el volante — patrón probado):** `CloneWheelAsShifter`, `CloneWheelAsHandbrake`, `SetupTurnSignals`, `ReplaceShifterCollider`…
- **UI/tema:** `EduThemeSetup`, `GenerateThemeAssets`, `RestyleHUD`, `RestyleDashboard`, `BuildFeedbackScreens`, `CreatePauseMenu`, `CreateSuccessToast`, `UIComposer`…
- **Building Blocks / wiring:** `MainMenuBuildingBlocks` (instala BBs de Meta por reflection), `MainMenuWiring` (Button.onClick por `UnityEventTools` — set_property de MCP no sirve para eso).
- **Fixes puntuales:** `FixAndroidXRLoader`, `FindBlockingColliders`, `EyeGazeSetup`…

---

## 10. Optimizaciones esenciales (Quest 3)

Las que sostienen el framerate — no tocar sin medir con el `FpsCounter`:

1. **Espejos a media velocidad** (`MirrorCamera`): las 3 cámaras de espejo están
   deshabilitadas y se llama `Render()` a mano cada 2 frames → **~50% menos costo** de
   la feature más cara del juego. Además: render monoscópico (`targetEye=None`),
   culling mask acotado (sin UI/manos/interior), far clip 40–60 m, FOV chico.
2. **Tráfico kinematic, sin física real:** los NPC se mueven con `MovePosition` +
   interpolación en FixedUpdate. Cero WheelColliders extra, velocidad estable ante
   drops de framerate. Los peatones son lerp puro, sin NavMesh que bakear.
3. **Capa "Traffic" aislada** (`TrafficLayerIsolator`): la matriz de colisiones se
   recorta por código — menos pares de colisión que evaluar por frame.
4. **Detección por triggers, no por polling:** todo el scoring son `OnTriggerEnter/Exit`;
   nada escanea la escena en Update.
5. **Sin allocations en el hot path:** delegates cacheados para suscribir/desuscribir
   al bus (AudioManager), `FieldInfo` de reflection cacheados estáticos (palancas),
   `InfractionRecord` como `readonly struct`, `sharedMaterial` en los guiños (no
   instancia materiales), early-returns en Update cuando no hay trabajo.
6. **Refresco configurable** (`DisplayRefreshRate`): default 90 Hz con fallback a la
   frecuencia más alta soportada; el usuario puede bajar a 72 si un nivel pesa.
7. **Confort VR como regla:** frenado suave en SafeFail, shake < 5 cm, doppler bajo en
   el motor, UI a 0.8–1.2 m — no es framerate, pero evita mareo igual que los FPS altos.
8. **Build:** occlusion culling bakeado + URP configurado para Quest.

---

## 11. Dónde tocar qué

- **Feature nueva que habla con el auto** → suscribite/publicá en `EventBus`. No referencies `Vehicle*` directo.
- **Nuevo detector de scoring** → heredá de `InfractionDetector` o `VehicleSensor`, trigger collider, y despachá al bus.
- **Nuevo nivel/misión** → assets `LevelDefinition` + misiones, sin código ([guía](GuiaLevelDesigner.md)).
- **Vehículo, volante, física, espejos, scoring, tráfico** → finalizados; no se tocan.
