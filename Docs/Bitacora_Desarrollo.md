# Bitácora de Desarrollo, SafeDriver

> Registro cronológico del desarrollo de **SafeDriver**, simulador VR de escuela de manejo para
> Meta Quest (Unity 6 / URP / Meta XR SDK). Documenta, por etapas y con fechas, los **cambios** que
> se fueron haciendo y los **errores/bugs** que surgieron y cómo se resolvieron, desde el primer
> commit hasta la entrega.
>
> **Período:** 30/03/2026 al 18/06/2026 · **97 commits**.
> Reconstruida a partir de la historia real del repositorio Git.

---

## Metodología y herramientas

- **Motor:** Unity 6 (URP), target Meta Quest 3 (Android / IL2CPP).
- **VR:** Meta XR All-in-One SDK, Building Blocks + Interaction SDK (se migró desde el stack OVR
  clásico durante el desarrollo).
- **Arquitectura:** un **EventBus** central desacopla las capas (Core, Vehicle, Scoring, Traffic,
  UI, Audio, Missions), cada una en su propia *assembly definition*.
- **Flujo de Git:** trabajo por **ramas** (al principio una por subsistema; luego *feature
  branches* que se mergean a `develop`). `main` quedó como base estable.
- **Testing:** validación en el headset (Quest 3) vía build/install; tests automáticos EditMode
  para el sistema de misiones.

> El proyecto pasó por un **pivote tecnológico** temprano: nació como prototipo de Realidad
> Aumentada (Vuforia) y se reorientó a Realidad Virtual para Meta Quest.

---

## Etapa 1. Génesis: prototipo AR/Vuforia y la decisión de pivotear a VR (30/03 al 13/04)

SafeDriver no nació como simulador VR. Arrancó como un prototipo de **Realidad Aumentada** sobre un
marcador físico: la idea inicial era una tarjeta/carta que, al ser detectada por la cámara, hiciera
aparecer una mini ciudad encima, con autos, peatones y semáforos con los que el usuario interactuaba
tocando la pantalla. Recién al final de esta etapa se decidió cambiar de rumbo hacia VR para Meta
Quest.

- **2026-03-30.** Se crea el proyecto Unity (`Initial commit`) sobre el template URP (render
  pipeline PC y Mobile, volume profiles, nuevo Input System, `SampleScene`). El commit `base` deja
  el proyecto fijado a su versión de Unity y ajusta settings de editor/graphics/shader graph para
  partir limpio.
- **2026-04-07.** `URP autofix?`: actualización de los URP RenderPipeline Assets de la v12 a la
  v13. El signo de pregunta refleja lo ocurrido: al abrir el proyecto, Unity disparó su **migración
  automática** de assets URP y reescribió los `.asset`. No fue un cambio buscado, sino una
  migración forzada por el editor que hubo que commitear para no arrastrar diffs sucios entre
  máquinas.
- **2026-04-12.** Comienza el prototipo AR:
  - **Base de Vuforia:** se integra **Vuforia Engine 11.4.4** (vendorizado como `.tgz`), su
    configuración y el script de migración. La escena se arma alrededor de un Image Target.
  - **Tarjeta AR + mini ciudad:** entra el grueso del contenido, el marcador, los modelos
    low-poly (`MiniCity_Complete.fbx`), autos, peatones, semáforos y sus materiales de luz en dos
    estados, más un `SceneBuilder` para armar la escena por código. `ARTouchInteraction` lanza un
    raycast ante un tap (móvil) o click (editor) y cicla el estado del semáforo tocado.
  - **Bug, modelos importados con rotaciones/pivotes mal:** los `.fbx` venían girados desde el
    exportador. Se corrigieron en **Blender** con una tanda de scripts Python (`fix_models.py`,
    `build_city_final.py`, `build_final_noparents.py`, etc.); este último sugiere que además hubo que
    **aplanar la jerarquía de padres** del FBX para que instanciara bien.
- **2026-04-13.** `UI interactiva para semáforos`: se reemplaza el control por tap directo por una
  **UI en pantalla** con tres scripts: `TrackingUIController` (muestra/oculta la UI según si el
  marcador está trackeado, con fade, resuelve que la UI no quede activa al perder la tarjeta),
  `TrafficLightButton` (botón con cooldown que evita el spam de pulsaciones durante el cambio de
  luces) y `TrafficLightIndicator`.

**El pivote a VR:**

- **2026-04-12.** `VR plug-ins`: se agregan al `manifest.json` los paquetes que definen el stack
  VR: **XR Interaction Toolkit 3.3.1, XR Management 4.5.4, XR Oculus 4.5.4 (loader de Quest), XR
  Hands 1.7.3, XR Composition Layers 2.3.0**, y los assets de configuración XR. Acá el proyecto
  deja de ser AR móvil y apunta a Meta Quest.
- **2026-04-12.** `Scene and XR settings`: entra el paquete de arte **SimplePoly City (Low Poly)**,
  que pasa a ser la base de ciudad para el entorno VR en lugar de la mini ciudad sobre marcador.
- **2026-04-13.** `Ciudad`: se crea la escena `Ciudad.unity`, primer escenario propio ya en clave
  VR. Este commit quedó como `origin/main` y cierra la génesis: el proyecto queda comprometido con VR
  sobre Quest, dejando atrás Vuforia.

> *Nota:* en esta etapa aún no existe la estructura `Assets/_SafeDriver/` ni las asmdef; todo vive
> suelto bajo `Assets/`. El armado de modelos se hacía en otra máquina.

---

## Etapa 2. Cimientos VR y subsistemas en paralelo (15/04 al 16/04)

En muy pocos días se levanta casi toda la base del simulador, trabajando por **ramas paralelas, una
por subsistema**. Acá nace la estructura definitiva bajo `Assets/_SafeDriver/` y las assemblies
(`Core`, `Audio`, `Scoring`, `Traffic`, `UI`, `VR`, `Vehicle`) con el `EventBus` como punto de
integración.

- **2026-04-15. Fix "VR not working".** El primer gran escollo: el proyecto no arrancaba en el
  Quest. Se completó la configuración nativa de Android/Oculus que faltaba: `AndroidManifest.xml`
  con la categoría VR, `uses-feature` de head-tracking como requerido, lista de dispositivos
  soportados (quest/2/pro/3/3s), registro del `OculusLoader` en XR Plug-in Management y settings de
  Meta XR. Con eso el headset empezó a renderizar y trackear.
- **2026-04-16. Configuración de Oculus y escena VR base.** Se crea la carpeta canónica
  `Assets/_SafeDriver/` y nace `Level_01_Basics.unity` como escena del nivel, migrando lo útil de la
  `SampleScene`. Entran los primeros modelos (`SafeDriver_Exterior_v1`, `SafeDriver_Interior_v1`) y
  `DrivingHaptics`. Luego un ajuste de piso para tener suelo y referencia espacial.
- **2026-04-16. Sistema de vehículo + núcleo (Core).** Llega el "Sistema de Vehículo Funcional"
  junto con `EventBus`, `GameManager`, `GameState` y los tipos de dominio (`ActionType`,
  `InfractionType`, `MirrorCheckRequirement`), más los primeros stubs de `ScoreManager`,
  `InfractionDetector`, `NPCPedestrianAI`.
- **2026-04-16. Sistema de infracciones.** Detectores por situación (`StopSignDetector`,
  `TrafficLightDetector`, `PedestrianCrossingDetector`, `SpeedLimitZone`), tabla de puntos
  (`ActionPoints`), estado de semáforo (`LightState`) y mejoras de feedback (`HapticsController`,
  `HeadTrackingDetector`, `HUDController`), todo conectado por `EventBus`.
- **2026-04-16. Sistema de score.** `ScoreManager` con `InfractionRecord` (registro de cada
  falta) y `LevelResult` (resumen final).
- **2026-04-16. UI pedagógica (Canvas World Space VR).** `HUDController`, `SpeedometerNeedle`,
  `LevelEndPanel`, `SafeFailScreen` y `VRCanvasSetup` para el canvas pointable en mundo.
- **2026-04-16. Tráfico y peatones.** `NPCPedestrianAI` y `NPCVehicleAI` con lógica de
  movimiento/cruce, y la interfaz `IPedestrianCrossingNotifier` para desacoplar el aviso de cruce.
- **2026-04-16. Audio (primer intento con FMOD).** El audio arrancó sobre **FMOD**
  (`FMODAudioManager`, `FMODEvents`, `SpatialAudioEmitter`) con su carpeta `FMODProject/`. *(Se
  revertiría a audio nativo en la Etapa 4.)*
- **2026-04-16. Steering (volante con controllers).** `SteeringWheelController`: mientras se
  mantiene el grip del controller, su yaw se mapea a la rotación del volante (con sensibilidad, zona
  muerta anti-drift y ángulo máximo); al soltar, vuelve suave al centro. La rotación normalizada
  `-1..+1` alimenta `VehicleInput`.

---

## Etapa 3. El volante inmersivo y su cadena de bugs (17/04 al 18/04)

**La etapa más densa en errores** del proyecto. El objetivo: convertir el volante en un objeto VR
**agarrable real** integrado con el Interaction SDK (ISDK), en lugar del input crudo de OVR. El
cambio destapó una cascada de bugs físicos y geométricos que se cazaron uno por uno.

- **2026-04-17. Reescritura del `SteeringWheelController`.** Se abandona el manejo manual por
  `OVRInput` y se delega el agarre, el snap de mano y la rotación al ISDK (`Grabbable` +
  `OneGrabRotateTransformer` + `GrabInteractable`/`HandGrabInteractable`). El script queda como mero
  lector del ángulo. (*Steering and Snap Feeling*)
- **2026-04-17. Bug: el volante se despega del auto.** El auto giraba bien, pero al moverse el
  chasis el volante se quedaba atrás (clásico problema de Rigidbodies anidados). Se resolvió en tres
  iteraciones:
  1. `FixedUpdate` que sincroniza el Rigidbody del volante + `IgnoreCollisionsWithParentRigidbody()`
     (*Auto Gira, Volante Bug*).
  2. Mover el pin a `LateUpdate` recalculando la pose world con `parent.TransformPoint` cada frame
     visual, para matar el jitter (*Fix volante parcial*).
  3. **Solución definitiva:** eliminar por completo el `Rigidbody` propio del volante, sus
     componentes ISDK apuntan al Rigidbody del auto y su collider pasa al *compound* del chasis. Sin
     Rigidbody anidado, desaparecen el despegue y el jitter (*Fix Volante*). Esto rompió el snap
     visual de la mano.
- **2026-04-17.** Para recuperar el snap se crea `SteeringWheelHandFollower`: pega la `HandVisual`
  al volante mientras se agarra y la hace seguir su rotación (desactivando `_updateRootPose` por
  reflection y restaurándolo al soltar). Soporta `GrabInteractor` y `HandGrabInteractor`. (*Steering
  Wheel Completely Immersive*)
- **2026-04-18. Fix: rango recortado al re-agarrar.** El `OneGrabRotateTransformer` guarda su
  ángulo en campos privados que persisten entre agarres, así que al reagarrar el rango útil quedaba
  recortado. Solución: mientras el volante **no** está agarrado, sincronizar esos campos
  (`_relativeAngle`, `_constrainedRelativeAngle`) por reflection con el ángulo visual real, para que
  el próximo agarre parta del pose actual con el rango completo.
- **2026-04-18. Fix: salto del steering al lado opuesto en el tope.** En unos 180 grados la
  precisión flotante hacía oscilar el ángulo y el unwrap manual lo saltaba entre +180 y -179.99,
  invirtiendo el steering de golpe. Se cambió `ReadAngle()` para leer directamente
  `_constrainedRelativeAngle` (ya signed/unwrapped/clampeado).

**La tanda de bugs de ruedas (todo el 18/04, en cadena):**

- **Fix: WheelColliders delanteros/traseros swapeados.** Los cuatro tenían los nombres cruzados
  respecto a su posición real, así que giraban las traseras y traccionaban las delanteras. Se
  renombraron por posición y se reasignaron los slots.
- **Fix: las mallas de ruedas no rotan ni siguen el steering.** Los slots apuntaban a *anchors*
  vacíos sin geometría. Se reasignaron a los meshes reales y se capturó en `Start` el offset de
  rotación inicial de cada mesh para preservar la orientación artística del modelo importado.
- **Fix: steering invertido tras corregir los WheelColliders.** Al mover el steering a las ruedas
  delanteras correctas, el signo quedó cruzado (la asignación errónea anterior lo disimulaba). Se
  negó el input en `ApplySteering` (`angle = -input * steerAngle`).
- **Espejos retrovisores (prototipo).** Central + 2 laterales: una `Camera` con `RenderTexture` por
  espejo, deshabilitada y renderizada manualmente cada N frames (`MirrorCamera`), monoscópica para
  abaratar costo en Quest. Editor tool `MirrorAssetCreator` para crear las texturas.
- **Palanca de marcha D/R con latch.** Máquina de estados de marcha (Drive/Reverse + estados de
  *arming*). El cambio D-R exige estar detenido y sostener el freno 2 s. La pieza clave es el latch
  `brakeReleaseRequired`: se activa también al pasar de moviéndose a detenido, para que el freno que
  el conductor venía sosteniendo *para frenar* no arme un cambio por accidente. Cableado del
  `GearIndicator` (TMP en el tablero) vía `EventBus.OnGearChanged`.

---

## Etapa 4. Ciudad, tráfico ambiental, UI, audio y build (20/04 al 26/04)

Esta etapa convierte el escenario de pruebas en una ciudad viva y deja el proyecto listo para correr
en Quest. El hito técnico es la **limpieza del stack VR clásico (OVR)** para consolidar todo sobre
Building Blocks / Interaction SDK.

- **2026-04-20.** Jornada intensa de UI y tráfico ambiental:
  - **Tráfico NPC:** se agregan dos autos con **giro en esquina por probabilidad** (`CarController`
    con `rightTurnWaypoints` + `rightTurnChance`; `Random.value < chance` decide recto o giro).
    Ajuste de balance posterior (0.4 a 0.5 en cruces principales, 0.35 en uno) para que el flujo no
    quede ni predecible ni caótico.
  - **Peatones:** botón para habilitar/deshabilitar el cruce; reescritura de `PedestrianController`
    para que caminen por las veredas en vez de quedarse quietos.
  - **UI:** rediseño ("UI Remake") con sprite redondeado; botón de encuesta wireado a
    `VideoPanelController` y ajuste de escala de botones.
  - **Build inicial:** primeros ajustes para Android/Quest (`app_icon`, Build Profile de Android,
    URP global settings).
- **2026-04-21. Limpieza del stack VR clásico (el cambio más importante de la etapa).** Se
  abandona OVR y se consolida toda la VR sobre **ISDK + Unity XR API**:
  - Se borra `SimpleWheelGrab` (usaba `OVRInput`) y se quita la referencia `Oculus.VR` del asmdef.
  - **Fix del grab del volante:** en el rig BB "Controller and Hand" el `transform` del interactor
    queda fijo en el TrackingSpace y solo el `WristPoint`/`Rigidbody` se actualizan, así que el
    filtro de proximidad **siempre** devolvía "fuera". Se corrige resolviendo la posición por la
    cadena `WristPoint`, `Rigidbody`, `transform`.
  - **Fix del steering que no llegaba al auto:** el `SteeringWheelController` estaba en la escena con
    `m_Enabled: 0`; su `Update` nunca corría. Se habilita.
  - Limpieza de código muerto y de carpetas fantasma; `.claude/` y `_Recovery/` al `.gitignore`.
- **2026-04-22.** Construcción de ciudad, audio nativo y limpieza:
  - **Arte:** se suman modelos de calle y semáforos, y un minimapa para playtest. Entra **SimplePoly
    City** como base con su lighting bake.
  - **Occlusion Culling:** se hornea `OcclusionCullingData`, paso clave de performance para Quest,
    que no puede renderizar la ciudad entera a la vez.
  - **Audio a nativo (se revierte FMOD):** FMOD era *overkill* para el scope educativo y arrastraba
    fricción. Se reemplaza por **AudioSource/AudioMixer nativo** (nuevo `AudioManager` con motor 3D
    modulado por velocidad+throttle, ambiente 2D, one-shots; nuevo `SpatialAudioEmitter`). Se borran
    los scripts FMOD; "Motor Sound" suma el clip y "Volume Bar" el mixer + `MasterVolumeController`.
  - **Limpieza:** se eliminan eventos huérfanos del `EventBus`, packages sin uso (Visual Scripting,
    Multiplayer Center, Collab-Proxy), se reemplaza `SampleScene` por `Level_01_Basics` en Build
    Settings, y se limpian warnings CS0414 (campos asignados nunca leídos).
- **2026-04-26.** Cierre de configuración de build (`OVRBuildConfig`, afinado de URP/ProjectSettings)
  para dejar un build instalable con la ciudad poblada, la UI rehecha, el audio nativo y el occlusion
  culling integrados.

---

## Etapa 5. Integración mayor: menú, niveles, pausa, tráfico NPC y tanda de fixes (06/05 al 12/05)

Salto de prototipos sueltos a un juego con **flujo completo**: menú principal navegable, nivel de
ciudad jugable con detectores reales, palanca D/N/R diegética, menú de pausa y tráfico NPC que
respeta semáforos y cruza la cebra. Se trabaja con *feature branches* que se mergean a `develop`.

- **2026-05-11. Día grande de integración:**
  - **MainMenu + Level_01_City + palanca D/N/R + objetivos diegéticos.** Escena de menú con botones
    Play/Quit wireados por código; nivel de ciudad con cartel PARE 3D, semáforo con materiales
    emisivos, paso peatonal y zonas de velocidad sobre los cruces reales. La lista de objetivos se
    muestra en el tablero (diegética) y se tacha al completar. La palanca D/N/R se **clonó del
    SteeringWheel** para un grab idéntico. Nuevas acciones: `PassedGreenLight` (+3) y
    `PedestrianNotPresent` (+3). *Detalle:* el `VehicleController` arranca en Neutral cuando hay
    shifter externo, para evitar una *race condition* con el `Start` del shifter.
  - **Vibración de tope (haptics) para volante y palanca + shifter con snap** (branch
    `grab-limit-feedback`): `GrabHaptics` (impulso por Unity XR, sin OVR), `CameraShake`, y
    `GrabLimitFeedback` (lee el *overshoot* del transformer por reflection, con *tiers*
    configurables y un último tier que fuerza el release + shake). *Nota:* los haptics de la palanca
    quedaron *inline* en la asmdef Vehicle para no crear un ciclo de asmdef (Vehicle no puede
    referenciar VR).
  - **Menú de pausa** (branch `pause-menu`): congela el tiempo y posiciona el canvas a 0.8 m frente
    a la cabeza **proyectando el forward al plano horizontal** para que no se incline. Toggle por
    botón B/Y.
  - **Tráfico ambiental por waypoints** (branch `traffic-and-pedestrians`): `TrafficWaypointPath`,
    `TrafficVehicle` (raycast forward para frenar ante el jugador) y `TrafficPedestrian`.
  - **Autos NPC respetan el semáforo** (branch `npc-respects-lights`): `TrafficLightStopZone` con
    trigger; el NPC frena suave si la zona activa está en rojo.
  - **Peatones cruzando + detector multi-notifier** (branch `crossing-pedestrians`): se introduce
    `IPedestrianCrossingMultiNotifier` (en Core, para romper el ciclo de asmdef Traffic con
    Scoring), con un `HashSet` de peatones presentes en vez de un bool, y un **rango de cebra
    bidireccional** (antes solo funcionaba en un sentido).
- **2026-05-12. Tanda de fixes finos de la palanca, surgidos del testeo en VR** (los bugs más
  sutiles del proyecto):
  - **La palanca bajaba sola a Neutral al arrancar:** el `ClampToZone` del *lock-when-moving*
    forzaba Neutral aunque estuviera en D/R. Fix: capturar la zona en la transición *stopped a
    moving* y clampear a esa misma zona. (También se agregó la referencia `Oculus.Interaction`
    faltante al asmdef Vehicle, y se corrigió la llamada inexistente `Dispatch_InfractionDetected`
    por `Dispatch_Infraction`.)
  - **El pulso de tope no aparecía:** el primer tier empezaba en `0` grados (ya "activo" al iniciar
    el grab). Fix: arrancar en `1` grado para que solo dispare con overshoot real. Rangos ampliados
    (volante x2, palanca x1.5).
  - **"Salto hacia atrás" del lock:** forzar el transform por script chocaba con la rotación del
    `OneGrabRotateTransformer`. Solución elegante: modificar dinámicamente los **Constraints**
    (`MinAngle`/`MaxAngle`) del transformer a la zona donde arrancó el auto, para que respete el
    límite sin pelearse con el script.
  - **Jitter de zona / snap a N al soltar:** con el constraint clampando a 20 grados exactos,
    `ZoneFor` devolvía Neutral (`20 > 20 = false`). Fixes: **margen de lock** (`lockMargin 1.5`),
    **histéresis** (`zoneHysteresis 2`) anti-flicker en el borde, y snap a la `lockedZone` al soltar.
    Además se creó el `_PauseMenu` que faltaba en la escena (no abría porque no existía).

Al cierre quedaron en `develop` el menú, el nivel jugable, la palanca D/N/R estable, el menú de
pausa y el tráfico ambiental, todo validado en Quest.

---

## Etapa 6. Arte, sistema data-driven, refactor de detección y UI nueva (17/05 al 01/06)

Salto de "prototipo jugable" a "producto con contenido y estilo propios", en cuatro frentes
paralelos.

**Arte de la ciudad:**
- **2026-05-17.** Se reemplazan los modelos sueltos por mallas **atlaseadas** (`Calles.fbx`,
  `Semaforos2.fbx`) con sus texturas, y se crean prefabs de tramo (`CalleRecta`, `CalleCurva`,
  `Interseccion`). Se renombran los materiales auto-generados a nombres legibles.
- **2026-05-19.** Edificios (`Edificios.fbx` + prefabs) para dar entorno urbano.

**Fixes y ajustes:**
- **2026-05-27. Bug del cuelgue en el loading (Android).** La build se quedaba colgada al iniciar.
  Causa: el `OculusLoader` no estaba habilitado para Android, así que el APK no incluía
  `libOVRPlugin.so` y `OVRManager` reventaba con `DllNotFoundException`. Solución: habilitarlo en
  `XRGeneralSettingsPerBuildTarget` + una editor utility (`Fix Android XR Loader`) que lo re-asigna
  si vuelve a desconfigurarse.
- **2026-05-27. Volante: recentrado proporcional a la velocidad.** El return-to-center escala por
  `CurrentSpeedKmh`: a 0 km/h queda quieto, a 30 km/h o más recentra a velocidad plena, simulando el
  self-aligning torque real.
- **2026-05-27. Fix del premio de frenar en rojo (primer intento).** El `TrafficLightDetector`
  marcaba `RanRedLight` apenas tocaba el trigger en rojo y bloqueaba el premio `StoppedAtRedLight`.
  Parche: marcar infracción solo si el auto entra moviéndose. (Se vuelve definitivo en el refactor
  del 29/05.)

**Sistema de misiones y niveles data-driven:**
- **2026-05-27.** Nace `SafeDriver.Missions`. El contenido deja de estar cableado en escena y pasa
  a **ScriptableObjects** con patrón Definition/Runtime: `MissionDefinition` base con variantes
  `Countable`/`Sequence`/`Timed`/`Compound`; `MissionManager` orquesta y expone eventos a la UI;
  `LevelDefinition` + `LevelManager` + `LevelProgress` manejan progresión, desbloqueo y best score
  (PlayerPrefs), con modo libre. `ObjectivesController` pasa a generar las filas en runtime desde el
  manager. Se documenta todo (`CrearMisiones.md`, `CrearNiveles.md`).
- **2026-05-28. Tests EditMode, 8/8 verde.** Nueva asmdef de tests con 8 casos NUnit que validan
  los 4 tipos de misión sin escena ni VR (contable cuenta/completa, secuencia avanza solo en orden,
  con-tiempo falla por timeout, compuesta agrega y propaga el fallo). Se llama `EventBus.Clear()`
  entre tests para no contaminar.

**Refactor del sistema de detección:**
- **2026-05-29.** Reescritura grande a un patrón unificado de **sensores**. Todos heredan de
  `VehicleSensor` (identifica al player de forma consistente) y aparece `CrossingLineSensor` (detecta
  el cruce de una línea y su dirección con un BoxCollider ancho, evitando el *tunneling* a alta
  velocidad). Cambios clave:
  - **Fix definitivo de "frenar en rojo no premiaba":** el semáforo se parte en `RedLightStopZone`
    (antes de la línea, premia y nunca penaliza) + `TrafficLightCrossLine` (sobre la línea: rojo =
    `RanRedLight` + SafeFail; verde = `PassedGreenLight`). El detector viejo queda `[Obsolete]`.
  - **Atropello real de peatón:** `PedestrianHitbox` sobre el cuerpo del peatón (`HitPedestrian`,
    grave). **Contramano:** `DirectionZone` (`WrongWay`). Se agregan ambos enums a `InfractionType`.

**Rediseño de UI:**
- **2026-06-01.** Sistema visual unificado. El `UITheme` (ScriptableObject) centraliza paleta de
  **señalética vial** (verde/rojo/ámbar/azul, fondos crema tipo manual del conductor), tipografía y
  métricas, aplicado con `UIThemeUtil`. Sobre eso, el rediseño "infantil-redondeado": fuentes
  redondeadas y sprite `RoundedRect` (*detalle de plataforma:* el sprite se generó **RGBA32 sin
  comprimir** porque comprimido no renderizaba en Quest); `SafeFail` y `LevelEnd` reconstruidos por
  composición; **interacción por POKE, no por ray**; se elimina el popup viejo y entra el
  `SuccessToast` (solo aciertos, arriba del volante).
- **2026-06-01. Panel de objetivos con scroll + fix de la hitbox del SafeFail.** El panel pasa a
  header fijo + viewport recortado con `RectMask2D` scrolleable por poke. El fix: el `SafeFailScreen`
  ahora **desactiva su contenido y su superficie de poke mientras está oculto**; antes la mano
  chocaba con una "pared invisible" al estirarse hacia el volante porque el collider de poke seguía
  activo.

---

## Etapa 7. Consolidación final: NPC sólidos, integridad, controles nuevos y entrega (12/06 al 18/06)

Tramo de cierre. Sobre una base estable se sumó el auto del player con IA y los NPC animados, se
consolidó en un único commit grande ("UI Remake") todo el trabajo final de jugabilidad y pulido, y
se cerró con el diseño de nivel. Buena parte de la etapa fue cazar bugs que solo aparecían con la
escena completa y en VR.

- **2026-06-12.** Se agrega el auto del player junto con la IA del tráfico ("Add car player and
  IA").
- **2026-06-15.** Se incorporan los NPC ("Add NPC") y, el mismo día, un **"Fix NPC animation":**
  las animaciones de los NPC no se reproducían; se corrigió la configuración de animación.
- **2026-06-18.** Se mergea a `develop` el commit grande **"UI Remake" (191 archivos)**, que
  consolida la jugabilidad final:

  **Tráfico NPC con colisión sólida.** Hasta acá los NPC eran triggers sin física (el player los
  atravesaba). Se pasó a colisión real: nueva capa física `Traffic` + `TrafficLayerIsolator` que en
  `Awake` ignora la colisión contra todas las capas menos sí misma (por código, sin tocar la matriz
  del proyecto). Así el hitbox sólido del player choca con los NPC pero no se engancha con calle ni
  edificios; el daño entra por `OnCollisionEnter` con punto de contacto real.

  **Error: los NPC se movían "a pasitos" / a velocidad inconsistente.** Se movían con
  `transform.position` en `Update` (dependiente del framerate, crítico en VR). Se reescribió
  `TrafficVehicle` para moverse por **Rigidbody kinematic con `MovePosition`/`MoveRotation` en
  `FixedUpdate`** (velocidad consistente) + `interpolation = Interpolate` (suavidad).

  **Error: los NPC frenaban sobre la senda o quedaban clavados en el cruce.** La detención por
  semáforo dependía de zonas-trigger, que dejaron de dispararse al aislar la capa `Traffic`. Se
  reemplazó por **líneas de detención por distancia** (`TrafficStopLine`): si una línea está en
  rojo, en su carril y todavía adelante, el NPC desacelera para parar justo antes
  (`vMax = sqrt(2*accel*d)`); si **ya pasó** la línea, la ignora y termina de cruzar. La detección
  de obstáculos pasó de un raycast fino a un `SphereCastAll`. Se agregó `CrosswalkTrafficGate`
  (por distancia) para que el peatón sepa si es seguro bajar del cordón.

  **Sistema de integridad del vehículo (8 zonas) + SafeFail por daño.** Nuevo `VehicleIntegrity`:
  clasifica cada impacto en una de 8 zonas (frente, atrás, 2 laterales, 4 ruedas) por el punto de
  contacto local, y el daño escala con la velocidad. Es aditivo (solo escucha colisiones). Dispara
  `SafeFail` (nuevo `InfractionType.SevereCollision`, grave) por golpe fuerte único o por integridad
  total crítica, con cooldown anti-spam. Se refleja en `VehicleDamageSymbol` (símbolo top-down
  verde a rojo + porcentaje total), en el tablero y en fin de nivel.

  **Error: la pantalla SafeFail mostraba la infracción ANTERIOR.** *Race condition* de orden de
  eventos: la UI cacheaba el tipo por evento, pero al transicionar a SafeFail el tipo nuevo todavía
  no estaba seteado. Se agregó `GameManager.TriggerSafeFail(type, message)`, que setea tipo y
  mensaje **sincrónicamente y luego** transiciona. De paso, los botones Retry/Menú pasaron a
  **recargar la escena** (antes `TransitionTo(Driving)` no reseteaba auto/score/misiones/timer) y se
  sumaron mensajes pedagógicos para `HitPedestrian`, `WrongWay` y `SevereCollision`.

  **Controles nuevos: freno de mano y luces direccionales.** Montados **clonando el SteeringWheel**
  (mismo stack de grab ya probado), vía editor scripts. `HandbrakeController` (palanca de 2
  posiciones, snap + háptico al enganchar, corta motor y aplica freno máximo) y luces direccionales
  (`TurnSignalStalk` de 3 posiciones + `TurnSignalController` que parpadea el par correspondiente,
  con indicadores en el tablero).

  **Pulido de UI y workflow de nivel.** Se corrigió que el cluster/panel de objetivos se despegaba
  del auto y que el timer salía del color del fondo (invisible). Se agregaron **prefabs de zonas de
  misión drag-and-drop** (`MissionZone_*`, `Pedestrian_Crossing`) para armar niveles sin código, el
  símbolo de daño en el tablero, y la documentación de entrega (`GuiaLevelDesigner` en md/html/docx).

- **2026-06-18.** Se agrega el **`LevelDesign`** final (commit en `origin/develop`), cerrando la
  etapa con el armado del nivel sobre todos los sistemas consolidados.

---

## Estado final del proyecto

Al cierre, SafeDriver es un simulador VR de manejo funcional con:

- **Conducción VR completa:** volante agarrable, acelerador/freno por gatillos, palanca D/N/R, freno
  de mano y luces direccionales, todos controles físicos.
- **Evaluación pedagógica:** puntaje (1000 inicial, aprobación 600), 9 tipos de infracción y 7 de
  acierto, pantalla **SafeFail** con cita a la Ley 24.449 (nunca muestra el choque), y boletín de
  fin de nivel.
- **Integridad del vehículo** por 8 zonas con SafeFail por daño.
- **Tráfico AI** (autos NPC con colisión sólida que respetan semáforos + peatones que ceden el
  paso).
- **Contenido data-driven:** misiones (4 tipos) y niveles como assets, con tests EditMode.
- **UI diegética** estilo manual del conductor, interacción por poke.
- **Documentación de entrega:** guía del level designer, GDD de mecánicas y esta bitácora.

**Integración Git al cierre:** todo el trabajo quedó consolidado en `feature/ui-apply` y mergeado a
`develop` (`5adc4ba`, "UI Remake"); sobre `origin/develop` se sumó luego `LevelDesign`. Las ramas
por subsistema de las primeras etapas quedan como historial del trabajo paralelo.

## Lecciones aprendidas (errores recurrentes)

- **Rigidbodies anidados en VR:** un grabbable con Rigidbody propio se despega del padre que se
  mueve. Patrón que funcionó: **sin Rigidbody propio**, compartir el del chasis y seguir la mano por
  `LateUpdate`.
- **Estado interno del `OneGrabRotateTransformer`:** sus ángulos privados persisten entre agarres y
  hay que **sincronizarlos por reflection** (rango recortado, salto en el tope, snap de palancas).
- **Triggers vs. aislamiento de capas:** aislar una capa de física por performance/colisión rompe
  toda detección basada en triggers de esa capa, hay que reemplazar por **detección por distancia**.
- **Movimiento dependiente del framerate:** mover por `transform` en `Update` da velocidad
  inconsistente en VR; conviene `MovePosition` en `FixedUpdate` + interpolación.
- **Race conditions de orden de eventos:** setear el estado **sincrónicamente antes** de transicionar
  (caso SafeFail).
- **Gotchas de plataforma Quest:** `OculusLoader` habilitado (si no, cuelga el loading), **Vulkan**
  como API gráfica, sprites **sin comprimir** para que rendericen, y hornear iluminación + occlusion
  culling.
- **El patrón "clonar el volante":** la forma confiable de crear nuevos grabbables compuestos en
  este proyecto (palanca de cambios, freno de mano, stalk de guiños, tablilla).
- **Desacople por EventBus + asmdefs:** permitió sumar feedback (audio, háptica, toasts) sin acoplar
  capas; cuando aparecía un ciclo de asmdef, se resolvió moviendo la interfaz a `Core` o dejando
  código *inline*.

---

*Bitácora reconstruida a partir de la historia completa del repositorio Git (97 commits,
30/03 al 18/06/2026), analizando los diffs reales de cada etapa.*
