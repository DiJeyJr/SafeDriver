# Resumen de estudio — Parcial de VR (Meta XR SDK + Diseño)

> Resumen completo para el parcial teórico. Combina: (1) los resúmenes de las clases en video,
> (2) la **verificación contra el código fuente real** del Meta XR Interaction SDK que está en el
> proyecto, y (3) la experiencia propia de haber construido **SafeDriver** (sim VR de manejo).
> Al final está la **práctica del profe resuelta** (Parte C) y **ejemplos propios** para defender
> en el oral (Parte D).
>
> Temario: herramientas del SDK de Meta (AIO / Building Blocks / Interaction SDK) + conceptos de
> diseño: **iluminación, game feeling, affordance, embodiment**.

---

# PARTE A — Meta XR All-in-One SDK (herramientas)

## A.1 Setup del proyecto

1. **Android Build Support** instalado en Unity (es Quest = Android).
2. **Meta XR All-in-One SDK** desde el Asset Store (paquete que junta core + interaction + platform).
3. **Plataforma → Android** en Build Settings.
4. **Project Setup Tool** (en *Oculus / Meta → Tools*): configura automáticamente los parámetros
   del proyecto (fix all).
5. **API gráfica → Vulkan** (no OpenGLES3): si falla la compilación en el visor, este suele ser el
   motivo.
6. **Modo desarrollador** activado en el casco desde la app móvil de Meta Quest.
7. Probar con **Oculus/Meta Link** (cable USB-C, juega desde el editor) o **Build and Run** (.apk
   al casco). Ojo: el comportamiento en el editor puede diferir del device — varias cosas de rig
   solo se validan con un **Build** real.

## A.2 Building Blocks — qué son

Piezas **modulares** que instalan funcionalidad sin armar todo a mano. Claves para el examen:

- Cada bloque tiene un **id (GUID)** estable; el instalador **resuelve dependencias por GUID**, no
  por nombre. Por eso instalar *Interactions Rig* arrastra solo el *Camera Rig*.
- Hay **singletons** (Camera Rig, Eye Gaze…): si ya existen, la instalación los **saltea** en vez
  de duplicar.
- Se acceden desde *Oculus/Meta → Tools → Building Blocks* (drag & drop a la escena).

### Catálogo (los principales)

| Categoría | Building Block | Qué hace |
|---|---|---|
| **Rig / tracking** | **Camera Rig** | Rig VR base (OVRCameraRig): cabeza + anclas de ojos/manos. Dependencia raíz. *Singleton.* |
| | **Controller Tracking** | Trackea y muestra los controladores Touch |
| | **Hand Tracking** | Tracking de manos desnudas (poner frecuencia en **High**) |
| | **Synthetic / Virtual Hands** | Manos virtuales que se "pegan" al objeto agarrado (no lo atraviesan) |
| | **Eye Gaze** | Eye tracking (Quest Pro): dirección de la mirada. *Singleton.* |
| | **Haptics** | Reproducción de vibración en los controllers |
| | **Controller Buttons Mapper** | Mapea botones a UnityEvents **sin código** |
| **Passthrough / MR** | **Passthrough** | Ver el mundo real por las cámaras (base de MR) |
| | Occlusion / Scene Mesh / Room Model | Oclusión real, malla de la sala, modelo semántico del cuarto |
| | Spatial Anchors | Anclar contenido a un punto del mundo real |
| **Interacción** | **Interactions Rig** | Rig comprehensivo de interacción (ISDK). Pulla el Camera Rig |
| | Hand/Controller Interactions, Real/Virtual Hands | Habilitan interacción por manos / controles |
| | **Interactores** (en la mano) | Grab, Hand Poke, Hand Ray |
| | **Interactables** (en el objeto) | HandGrab, Distance Grab, **Grabbable Item**, **Ray Interaction (Pointable Item)**, **Poke Interaction**, Throwable |
| **Locomoción** | **Teleport** | Locomoción por **teletransporte** (apuntar y aparecer) |
| **Otros** | Spatial Audio, Virtual Keyboard | Audio posicional Meta, teclado VR |

> **Trampa de nomenclatura:** *Ray Interaction* en el archivo se llama *Pointable Item*; *Virtual
> Hands* en el archivo es *Synthetic Hands*. En el examen conviene nombrar ambos.

## A.3 Arquitectura de Interacción (el modelo mental)

El Interaction SDK separa tres roles:

- **Interactor** → va en la **mano/control** (ej. `HandGrabInteractor`, `PokeInteractor`,
  `RayInteractor`). Es **quién** actúa.
- **Interactable** → va en el **objeto** (ej. `HandGrabInteractable`, `PokeInteractable`). Es
  **qué** se puede accionar.
- Entre ellos corre una **máquina de estados**: `Normal → Hover → Select`. Hover = lo estás
  apuntando/tocando; Select = lo agarraste/apretaste.

**Pointable / PointerEvent:** muchos componentes emiten un `PointerEvent` con un `Type`
(`Hover`, `Unhover`, `Select`, `Unselect`, `Move`, `Cancel`). Es el canal de bajo nivel que usan
los **event wrappers**.

### Event Wrappers (clave para el feedback por eventos)

Son `MonoBehaviour` que se suscriben a un objeto de interacción y re-exponen sus eventos como
**`UnityEvent`** cableables desde el Inspector → permiten disparar **sonido, partículas, activar
GameObjects sin escribir código**.

| Wrapper | Envuelve | Eventos que expone |
|---|---|---|
| **`PointableUnityEventWrapper`** | un `IPointable` (ej. el `Grabbable`) | `WhenHover`, `WhenUnhover`, **`WhenSelect`**, `WhenUnselect`, `WhenMove`, `WhenCancel`, `WhenRelease` — cada uno pasa el `PointerEvent` |
| **`InteractableUnityEventWrapper`** | un `IInteractableView` (ej. `HandGrabInteractable`) | `WhenHover/Unhover/Select/Unselect` (sin parámetro) + `WhenInteractorViewAdded/Removed`, `WhenSelectingInteractorViewAdded/Removed` |
| **`PointableCanvasUnityEventWrapper`** | un `IPointableCanvas` (UI uGUI) | eventos de highlight/select del `PointableCanvasModule` |

Matices de examen:
- `WhenSelect` = **al agarrar**. `WhenRelease` **≠** `WhenUnselect`: en `Unselect` siempre se
  invoca `WhenUnselect`, y `WhenRelease` **solo además** si el puntero seguía sobre el objeto (no
  se soltó "afuera").
- Cada wrapper tiene **un campo de referencia obligatorio** (`_pointable` / `_interactableView` /
  `_pointableCanvas`) que **hay que asignar** en el Inspector. Si queda sin asignar (o apuntás al
  GameObject equivocado), en `Start` salta un **`AssertField`**, el componente **no arranca** y
  **nunca se suscribe** → "no pasa nada" silencioso (ver práctica P4).

## A.4 Grab interactions (agarre)

**Método automático:** menú contextual *Interaction SDK → Add Grab Interaction*. El asistente
agrega solo el **`Rigidbody`**, el **`Collider`** y el **`GrabInteractable`/`HandGrabInteractable`**.

**Método manual (estructura recomendada):** un objeto **Root** con sub-objetos para *visuals,
colliders, hand grab, controller grab y sfx*. Componentes clave:

- **`Grabbable`** → define si el objeto es afectado por físicas (gravedad/cinemática) y si permite
  **transferencia entre manos**. (Internamente es un `IPointable`.)
- **`HandGrabInteractable`** + **`ControllerGrabInteractable`** → reglas de agarre (gestos
  **pinch** o **palm**), para manos y para controles.
- **`HandGrabVisual`** (vincula la mano sintética para mostrar la pose) y **`HandGrabGlow`**
  (brillo/contorno al interactuar).

### Flujo: AGARRAR con la mano → SONIDO (pregunta típica)

```
[objeto con Rigidbody + Collider + Grabbable + HandGrabInteractable]
   -> agregar PointableUnityEventWrapper, campo _pointable = el Grabbable
   -> evento WhenSelect (al agarrar)
   -> arrastrar el GameObject con el AudioSource al evento
   -> elegir la función AudioSource.Play()
```

## A.5 Hand Grab Poses (poses de mano)

Definen **cómo** se ve la mano al agarrar (rotación de cada dedo) y dónde **encaja** (snap).
**Cuatro componentes** y dónde viven:

| Componente | Tipo | Dónde va | Qué hace |
|---|---|---|---|
| **`HandGrabInteractable`** | MonoBehaviour | **objeto** (raíz, junto al Rigidbody) | Declara que se puede agarrar; guarda la lista de poses |
| **`HandGrabPose`** | MonoBehaviour | **hijo** del objeto | Punto/orientación local donde se alinea la mano |
| **`HandPose`** | clase `[Serializable]` (**no** es componente) | **embebida** en el `HandGrabPose` | Datos: handedness, rotaciones de joints, **libertad de dedos** |
| **`HandGrabInteractor`** | MonoBehaviour | **la mano** del rig | Detecta y ejecuta el agarre; puntúa las poses y snapea a la mejor |

> Regla mental: **Interactable + Pose → en el OBJETO; Interactor → en la MANO.**

**Libertad de dedos (`JointFreedom`)**, uno por dedo:
- **`Free`** (libre): el dedo no se fuerza, sigue el tracking real.
- **`Constrained`** (restringido): se mueve pero acotado a la pose.
- **`Locked`** (bloqueado): se clava en la rotación de la pose.

**Flujo de pasos:**

```
[objeto: Rigidbody + Collider + Grabbable]
   -> agregar HandGrabInteractable (autobusca Rigidbody/Grabbable en el padre)
   -> crear HandGrabPose como HIJO (Add HandGrab Pose)
   -> dejar usesHandPose = true + asignar HandGhostProvider (mano fantasma de preview)
   -> en SceneView colocar la pose y, en "Edit fingers", ajustar JointFreedom + rotar joints
   -> (opcional) "Create Mirrored HandGrabInteractable" para la mano opuesta
```

## A.6 Interacción multimodal (manos + controles a la vez)

Combina la inmersión del hand tracking con la precisión/háptica de los controles. Configuración
en el **OVR Manager** del Camera Rig:

- **Controller Driven Hand Type → Conform to Controller**.
- Activar **Simultaneous Hands and Controllers**.
- Hand Tracking con **show state = Always**.
- Conectar **OVR Hand Data Source** con el controller; configurar los **Synthetic Hands** en los
  *Visuals* para que el sistema sepa qué mano usa qué input.
- Agregar un **`HandGrabInteractor`** a cada mano; el **Controller Pinch Injector** y los grupos de
  interactores hacen que el agarre funcione con cualquier input.

## A.7 UI en VR

- **Canvas en World Space** (obligatorio) a **escala 0.001** (1 unidad de canvas = 1 mm).
- Click derecho sobre el Canvas → *Interaction SDK* agrega **Ray Interaction** (a distancia) o
  **Poke Interaction** (tocar con el dedo). Internamente: `PointableCanvas` + un
  `PointableCanvasModule` en el `EventSystem` (sin él, `Button.onClick` no dispara).
- Estructurar con **Vertical/Horizontal Layout Group** + **TextMeshPro**; **Scroll View** para
  listas; **Toggle** + **Layout Element** para botones de tamaño fijo (UI espacial estilo Vision
  Pro: fondo con esquinas redondeadas semitransparente).
- La lógica se cablea con los **eventos de Unity** (`OnClick`, `OnValueChanged`).
- **Poke vs Ray:** poke = contacto físico directo (botones cercanos); ray = puntero a distancia.

## A.8 Locomoción

**Teletransporte (Building Block `Teleport`):** define **dónde** puede ir el jugador. Tres formas
de crear zonas válidas:
- **NavMesh Surface** — áreas amplias (un piso); requiere **Bake** de la malla.
- **Collider Surface** — áreas puntuales/inclinadas (un Box Collider que el sistema reconoce).
- **Áreas inválidas** (el vacío) — `score = -10` en el `Teleport Interactable` para que no se pueda
  teletransportar ahí.
- **Teleport Points** (`Target Point`) — destino exacto + rotación final de llegada.
- Componentes: `TeleportInteractable`, `ReticleDataTeleport` (el cursor), `ColliderSurface`.

**Caminar room-scale con colisión (no atravesar paredes):** lo dan los componentes
**`FirstPersonLocomotor` + `CharacterController`** (una **cápsula física con colisión**). ⚠️ **No
hay un Building Block de un clic** para esto: se monta con el **prefab `Locomotor.prefab`** del
sample de Locomotion. El único BB rotulado "Locomotion" es **Teleport** (que es teletransporte).

## A.9 Passthrough / Realidad Mixta

- Building Block **Passthrough**, o a mano: en el **OVR Manager** activar **Passthrough Support** +
  **Enable Passthrough**.
- En el **Center Eye Anchor**, **Clear Flag → Solid Color** (negro con **alpha 0**) para que se vea
  el mundo real.
- Componente clave: **`OVRPassthroughLayer`** (renderiza y compone la capa real con la virtual).
- Tipos de composición:
  - **AR (reconstruido):** la imagen real es el **fondo** de toda la escena (se le pueden aplicar
    estilos, ej. efecto Matrix).
  - **Surface projection:** la imagen real solo se proyecta sobre **geometrías específicas** (un
    quad) → ventanas/áreas concretas.
  - **Windows (área):** "ventanas" al mundo real usando un **shader Selective Passthrough** + la
    capa **Transparent**.

## A.10 Eye Gaze (mirada)

- Building Block **Eye Gaze** (requiere **Quest Pro**; depende del Camera Rig; *singleton*).
- Instancia el componente **`OVREyeGaze`**. La elección de ojo se hace **sin código**, con el campo
  **`Eye`** (enum **`EyeId { Left, Right }`**).
- Campos del Inspector: **`ApplyRotation`** (el objeto rota siguiendo la mirada), `ApplyPosition`,
  `ConfidenceThreshold`, `ReferenceFrame`.

## A.11 Build y gotchas

- **Vulkan** (no OpenGLES3) para evitar fallos de compilación en el visor.
- **Build and Run** genera el `.apk`. Varias configuraciones de rig **solo se validan en el
  device**, no en el editor.

---

# PARTE B — Conceptos de diseño

## B.1 Game feel / "juice"

**Steve Swink (2009):** game feel = **"el control en tiempo real de objetos virtuales en un
espacio simulado, con las interacciones enfatizadas mediante pulido (polish)."** Tres pilares:

1. **Control en tiempo real** — el jugador manipula frame a frame, con latencia mínima. En VR es
   literal: tu mano *es* el input.
2. **Simulación espacial** — hay un espacio con reglas físicas coherentes (colisiones, inercia,
   peso). El cerebro "compra" el espacio.
3. **Pulido (polish) / "juice"** — capas de feedback que **enfatizan la interacción sin cambiar la
   simulación**. La pelota rebota igual matemáticamente, pero el *squash & stretch* + sonido +
   partícula te hacen *sentir* el rebote.

**Capas de feedback:** visual (animación con anticipación→acción→follow-through, squash&stretch,
flash, hit-stop), **audio** (el más barato y subestimado: *thunk* = peso, *click* = precisión),
**háptico** (en VR es **central**, sustituye al tacto), **partículas**, **anticipación** (micro
windup). **Screen shake: PROHIBIDO en VR** (mover el horizonte = mareo) → se reemplaza por shake
del *objeto*, del HUD diegético o un pulso háptico.

**Tip de examen:** citá las **3 palabras de Swink** (control en tiempo real + simulación espacial
+ pulido) y la frase *"el polish enfatiza sin alterar la simulación"*.

## B.2 Feedback positivo vs negativo

- **Positivo** (recompensa): confirma lo correcto, refuerza la conducta.
- **Negativo** (castigo/error): informa lo que salió mal, corrige la conducta.

> *Aclaración:* en teoría de sistemas "negativo" = autorregulación y "positivo" = amplificación.
> Acá hablamos en sentido **UX: recompensa vs castigo**. Conviene aclararlo en el examen.

**¿Por qué hay que tener MÁS cuidado con el negativo?**

1. **Sesgo de negatividad / aversión a la pérdida:** lo malo pesa psicológicamente más que lo
   bueno. Un castigo mal calibrado "arruina" la sesión aunque haya habido muchos aciertos. El
   positivo **perdona el exceso**; el negativo no.
2. **Frustración e injusticia:** nadie se queja de una recompensa de más; el castigo dispara
   *"¿fue mi culpa?"*. Si es **desproporcionado, abrupto o por algo fuera del control del jugador**
   (tracking impreciso), se siente injusto. Debe ser **legible, anticipable y proporcional**.
3. **Castigo vs información:** buen feedback negativo **informa** qué hacer distinto, no solo
   "perdiste".
4. **Confort físico en VR (lo más crítico):** flashes de pantalla completa, vignette rojo agresivo,
   screen shake y *knockback de cámara* → **mareo / cybersickness**; sonidos de error fuertes
   espacializados en la cabeza → sobresalto amplificado. El negativo en VR debe ser **suave,
   diegético y sin tocar la cámara**.
5. **No romper inmersión/embodiment:** un "GAME OVER" gigante te saca del mundo → rompe la
   presencia. Ideal: que el castigo viva dentro de la ficción.
6. **Accesibilidad:** si la señal de error es *solo* color → un daltónico la pierde; *solo* sonido
   → alguien con hipoacusia. **Redundancia multicanal** obligatoria en feedback crítico.

**Síntesis:** el positivo refuerza con **margen de error amplio**; el negativo opera contra el
sesgo de negatividad, puede sentirse injusto, **marea** en VR y **rompe inmersión** → debe ser
**proporcional, anticipable, informativo, cómodo, diegético y multicanal**.

## B.3 Affordance y signifiers (Gibson y Norman)

- **Affordance (Gibson, 1979):** la **relación objeto–agente** que determina qué acciones son
  *posibles* (una silla "ofrece" sentarse). **Existe objetivamente**, se perciba o no.
- **Norman** lo llevó al diseño y distinguió:
  - **Affordance real:** lo que el objeto efectivamente permite.
  - **Affordance percibida:** lo que el usuario *cree* que puede hacer al mirarlo.
  - **Signifier (significante):** la **señal perceptible** que comunica dónde/cómo actuar. La
    manija de una puerta no es la affordance: es el **signifier**. El "Norman door" (manija de
    tirar en una puerta que se empuja) es un signifier que **miente**.

**En VR importan más** (el usuario espera usar sus manos como en la vida real, sin teclado que
explique): **forma = affordance** (algo del tamaño de la mano con un asa se ve agarrable); **glow
/ outline de hover** = el signifier digital por excelencia (equivale al cursor del escritorio);
**manos virtuales que cambian de pose** anticipan la acción. Un interactivo **sin** signifier es
invisible; un no-interactivo que *parece* agarrable y la mano lo atraviesa = **promesa rota** que
rompe la presencia.

**Tip de examen:** los **tres niveles de Norman** (real / percibida / signifier) + *Gibson dice
que la affordance existe en la relación objeto-agente; Norman trabaja la percibida vía signifiers*.

## B.4 Embodiment / encarnación y presencia

**Embodiment** = la sensación de que un cuerpo virtual **es tu cuerpo**. **Kilteni, Groten &
Slater (2012)** — marco canónico — lo descomponen en **tres componentes**:

1. **Body ownership** ("este cuerpo es mío") — el cerebro adopta las manos/avatar virtuales (cf.
   *rubber hand illusion*).
2. **Agency** ("yo lo controlo") — ser el autor de los movimientos; depende de **baja latencia** y
   **correspondencia motora**.
3. **Self-location** ("estoy ubicado dentro de este cuerpo / lugar").

**Inmersión vs presencia:** *inmersión* = capacidad **técnica** del sistema (FOV, tracking, audio
espacial); *presencia* = la **respuesta psicológica** de "estar ahí". Embodiment y presencia se
retroalimentan.

**Cómo se rompe (break-in-presence):** latencia (mata la agency), la **mano que atraviesa objetos
sólidos** (rompe ownership), mover/teletransportar la cámara sin tu intención (rompe self-location
+ marea), UI 2D pegada a la cara / menús no diegéticos, avatar en pose imposible.

**Mareo (cybersickness):** sobre todo por romper **agency/self-location** — el conflicto
**vestíbulo-visual** (los ojos ven movimiento que el oído interno no siente). Regla: **no mover la
cámara** salvo que el usuario lo comande con su cuerpo. (Caso delicado del sim de manejo: el auto
se mueve y el cuerpo está quieto → se mitiga con velocidad estable, aceleraciones suaves, horizonte
fijo.)

**Tip de examen:** los **3 de Kilteni** (ownership / agency / self-location) y a qué ruptura se
asocia cada uno; diferenciar **inmersión (técnica)** de **presencia (psicológica)**.

## B.5 Iluminación para VR / Unity URP

**Modos de luz:**
- **Realtime:** se calcula cada frame; sombras dinámicas; **cara** en mobile/Quest.
- **Baked (horneada):** se **precalcula** en el editor y se guarda en **lightmaps** (la luz
  "pintada" sobre la geometría estática); costo en runtime casi nulo, pero **no** reacciona a lo
  que se mueve.
- **Mixed:** combina — directa realtime (sombras de dinámicos) + indirecta/GI horneada.

**Herramientas:**
- **Lightmaps** → luz horneada de la geometría **estática** (objetos *Static / Contribute GI*).
- **Light Probes** → como los lightmaps son solo para estáticos, los **objetos dinámicos** (auto,
  peatones) reciben la GI interpolando entre **muestras de luz** capturadas en el espacio. *(Unity 6
  / URP: **Adaptive Probe Volumes (APV)** automatizan esto.)*
- **Reflection Probes** → cubemap del entorno para **reflejos** (carrocería, vidrios).
- **Light Cookies** → una **textura que modula/recorta la luz** proyectada (como un *gobo* delante
  del foco): proyecta patrones (persiana, follaje, ventana) **sin geometría real**. Es exactamente
  el video de Blender: en vez de modelar y calcular sombras caras, **horneás el patrón en una
  cookie** y lo proyectás barato.

**Costo en Quest (mobile):** las luces realtime con sombras son **carísimas**. Presupuesto típico:
**una sola direccional realtime (el sol, en modo Mixed)** y **todo lo demás horneado**. Bakear
convierte iluminación en **lookup de textura** (lo que la GPU móvil hace mejor). Dinámicos →
**Light Probes / APV** para que no se vean "despegados" del fondo.

**Cómo la luz sostiene los otros conceptos:** **affordance** (la luz **dirige la atención** y
resalta lo interactivo — es un signifier ambiental), **embodiment/presencia** (sombras y reflejos
coherentes son **claves de realidad**; una sombra bajo tu mano refuerza la self-location), **mood**
(temperatura/contraste = tono) y **legibilidad** (que el alumno vea bien señales y tráfico).

**Tip de examen:** *"en Quest se hornea todo lo posible porque la luz realtime es el principal
costo de GPU; bakear = convertir luz en textura"* + el trío **lightmaps (estáticos) + light
probes/APV (dinámicos) + reflection probes (reflejos)** + **light cookie = textura que recorta la
luz**.

---

# PARTE C — Práctica del profe (resuelta y verificada)

> Las 5 respuestas fueron verificadas contra el **código fuente real** del SDK
> (`PointableUnityEventWrapper.cs`, `HandGrabPose.cs`, `CharacterController.cs`,
> `FirstPersonLocomotor.cs`, `OVREyeGaze.cs`).

### 1) Flujo para relacionar un SONIDO con AGARRAR un objeto con las MANOS

**Objeto agarrable (`Rigidbody` + `Collider` + `Grabbable` + `HandGrabInteractable`) → agregar
`PointableUnityEventWrapper` con `_pointable` = el `Grabbable` → evento `WhenSelect` → arrastrar el
GameObject con el `AudioSource` → función `AudioSource.Play()`.**

Pasos:
1. El objeto debe poder agarrarse: `Rigidbody` + `Collider` + `Grabbable` + `HandGrabInteractable`
   (esto permite que el `HandGrabInteractor` de la mano llegue al estado **Select**).
2. Agregar el **`PointableUnityEventWrapper`** y en su campo **`_pointable`** arrastrar el
   **`Grabbable`** (vale porque `Grabbable` es un `IPointable`). *Alternativa equivalente:*
   `InteractableUnityEventWrapper` con `_interactableView` = el `HandGrabInteractable`.
3. Agregar un **`AudioSource`** (con *Play On Awake = false*).
4. En el evento **`WhenSelect`**: `+` → arrastrar el GameObject del `AudioSource` → función
   **`AudioSource.Play()`**.

⚠️ Usar **`WhenSelect`** (al agarrar), **no** `WhenRelease` (eso sería al soltar).

### 2) ¿Por qué más cuidado con el feedback NEGATIVO que con el positivo?

Porque el negativo **castiga**, y un castigo mal calibrado deteriora la experiencia más rápido de
lo que un premio la mejora: **(a)** sesgo de negatividad (lo malo pesa más; el positivo perdona el
exceso, el negativo no); **(b)** riesgo de sentirse **injusto** si es desproporcionado, abrupto o
por algo fuera del control del jugador; **(c)** en VR puede **marear/sobresaltar** físicamente
(flashes, screen shake, knockback de cámara, audio fuerte); **(d)** puede **romper la inmersión/
embodiment** (un GAME OVER que te saca del mundo); **(e)** debe ser **informativo, no punitivo**
(decir qué corregir). Por eso el negativo se planifica **proporcional, anticipable, legible,
cómodo, diegético y multicanal**; el positivo tiene margen de error amplio.

### 3) Flujo para establecer POSES DE MANOS — componentes y objetos

Componentes (regla: **Interactable + Pose → en el OBJETO; Interactor → en la MANO**):
- **`HandGrabInteractable`** (MonoBehaviour) en el **objeto raíz**, junto al `Rigidbody`.
- **`HandGrabPose`** (MonoBehaviour) como **hijo** del objeto.
- **`HandPose`** (clase `[Serializable]`, **no** componente) **embebida** dentro del `HandGrabPose`
  (campos de dedos: `JointFreedom` = `Free`/`Constrained`/`Locked`).
- **`HandGhostProvider`** para la **mano fantasma (ghost)** de preview.
- **`HandGrabInteractor`** en la **mano** del rig.

Pasos: objeto con `Rigidbody`+`Collider`+`Grabbable` → agregar `HandGrabInteractable` → crear
`HandGrabPose` como **hijo** ("Add HandGrab Pose") → `usesHandPose = true` + asignar
`HandGhostProvider` → colocar la pose en el SceneView y, en **"Edit fingers"**, ajustar la libertad
y rotar los joints → *(opcional)* **"Create Mirrored HandGrabInteractable"** para la otra mano.

### 4) Puse un *event wrapper* con un evento de partículas en *Select* y "no pasa nada". ¿Por qué?

Causas, de la más común a la más sutil:
1. **La referencia del wrapper quedó sin asignar o apunta al objeto equivocado** (causa #1). En
   `Start` el wrapper hace `AssertField` sobre `_pointable`/`_interactableView`; si está nulo (o
   arrastraste el root vacío en vez del **`Grabbable`/`HandGrabInteractable`**), la assertion salta,
   el componente **no se inicia** y **nunca se suscribe** → "no pasa nada" silencioso. **Mirá la
   Console:** un `AssertField` rojo lo confirma.
2. **El objeto nunca llega a Select** porque le falta lo de aguas arriba (sin
   `Grabbable`/`HandGrabInteractable` real, o sin `Rigidbody`/`Collider`).
3. **Falta el interactor en la mano** (`HandGrabInteractor`): nadie selecciona el objeto.
4. **Evento en el campo equivocado** (`WhenRelease`/`WhenUnselect` en vez de `WhenSelect`).
5. **El `ParticleSystem` no se reproduce** (no llamaste a `ParticleSystem.Play()`, o apunta al
   componente equivocado).
6. **El componente/GameObject del wrapper está deshabilitado** → `OnEnable` no corre.

### 5) Sin programar: no atravesar paredes con el caminar real + mecánica por ojo izq/der

**(a) No atravesar paredes/superficies (room-scale):** los componentes **`FirstPersonLocomotor`
+ `CharacterController`** (una **cápsula física con colisión**: `CapsuleCollider`, `LayerMask` de
qué bloquea, slopes y steps; resuelve la colisión **deslizando** sobre las superficies). El
`FirstPersonLocomotor` mantiene esa cápsula **sincronizada con el jugador real**, así el caminar
natural no atraviesa geometría. Se integra con el **prefab `Locomotor.prefab`** del sample de
Locomotion. ⚠️ **No hay un Building Block de un clic** para esto; el único BB de locomoción es
**Teleport** (teletransporte, no caminar con colisión).

**(b) Mecánica por ojo:** **Building Block "Eye Gaze"** (requiere Quest Pro; depende del Camera
Rig), que agrega **`OVREyeGaze`**; el ojo se elige **sin código** con el campo **`Eye`** = `Left`
o `Right` (enum `EyeId`). Para ambos ojos por separado, dos GameObjects con `OVREyeGaze`, uno
`Left` y otro `Right`.

---

# PARTE D — Cómo lo aplicamos en SafeDriver (ejemplos propios)

Ejemplos defendibles "porque lo construimos así" — útiles para dar respuestas de primera mano.

**Flujo grab + feedback (vía EventBus):** al enganchar el freno de mano o la palanca de guiños,
sale un **pulso háptico solo en la mano que lo sostiene** (`GrabHaptics` resuelve qué interactor
está en `Select`). Y todo el feedback de acierto/infracción está **desacoplado por un EventBus**:
chequear un espejo despacha `CheckedMirrorsBeforeTurn` y reaccionan háptica, audio y toast **sin
conocerse** entre sí.

**Game feel / juice:** **háptica escalonada** (`GrabLimitFeedback`: cuanto más forzás un control
más allá de su tope, más fuerte vibra), **snap de palancas** (lerp de ~0.15 s a la posición
discreta), **recentrado del volante proporcional a la velocidad** (simula el self-aligning torque),
**camera shake corto y decreciente** al perder un control forzado.

**Feedback negativo cuidado:** **SafeFail no muestra el choque** (congela y enseña con el artículo
de la Ley 24.449); **frenado suave** en vez de frenazo (el estado SafeFail a propósito *no* congela
el tiempo, para poder rampar la desaceleración); **shake acotado < 5 cm** para no marear; háptica
de infracción "alerta sin susto" (doble golpe corto, no zumbido agresivo).

**Affordance:** **filtros de proximidad** (`WheelGrabProximityFilter`) para que el volante solo se
agarre **donde se ve agarrable** (no desde cualquier punto del auto); controles físicos con forma
reconocible y posiciones discretas (D/N/R, freno arriba/abajo) que comunican "esto cambia de
estado".

**Embodiment:** la **mano virtual se "pega" al objeto** (`GrabbableHandFollower` /
`SteeringWheelHandFollower`): toma un snapshot de la pose y cada `LateUpdate` fuerza la mano al
volante, evitando que flote o lo atraviese — sostiene ownership + agency.

**Eye gaze / head tracking:** chequeo de espejos por **giro de cabeza** (Quest 3, principal) o por
**mirada real** (`OVREyeGaze`, Quest Pro, fallback graceful); ambos despachan el **mismo evento**.

**UI world-space poke:** canvas a escala 0.001 a distancia de confort, botones por **poke**; el
**hitbox se apaga** cuando el panel está oculto para no chocar con una "pared invisible".

---

# Apéndice — Chuleta de nombres y frameworks

| Tema | Recordá |
|---|---|
| **Game feel** | Swink: control tiempo real + simulación espacial + **pulido** (enfatiza sin alterar la simulación) |
| **Embodiment** | Kilteni: **ownership + agency + self-location** |
| **Affordance** | Norman: real / percibida / **signifier** (Gibson: existe en la relación objeto-agente) |
| **Feedback** | El negativo: proporcional, anticipable, informativo, cómodo (VR), diegético, multicanal |
| **Luz en Quest** | Bakear todo; lightmaps (estáticos) + light probes/APV (dinámicos) + reflection probes; cookie = textura que recorta la luz |
| **Event wrapper** | `PointableUnityEventWrapper` (IPointable, `WhenSelect`) / `InteractableUnityEventWrapper` (IInteractableView). Necesitan su referencia asignada o "no pasa nada" |
| **Grab** | `Grabbable` + `HandGrabInteractable` (objeto) ↔ `HandGrabInteractor` (mano) |
| **Hand pose** | `HandGrabInteractable` + `HandGrabPose` (hijo) + `HandPose` (embebida, JointFreedom Free/Constrained/Locked) + `HandGrabInteractor` |
| **No atravesar paredes** | `FirstPersonLocomotor` + `CharacterController` (prefab `Locomotor.prefab`); **no** hay BB (solo Teleport) |
| **Mecánica por ojo** | BB **Eye Gaze** → `OVREyeGaze.Eye` = `Left`/`Right` |
| **UI VR** | Canvas **World Space** escala **0.001** + Ray/Poke Interaction + PointableCanvasModule |
| **Passthrough** | Enable Passthrough + Center Eye Clear Flag = Solid Color (alpha 0) + `OVRPassthroughLayer` |
| **Build** | API gráfica **Vulkan** |

---

*Resumen generado combinando los resúmenes de las clases en video, la verificación contra el código
fuente del Meta XR Interaction SDK del proyecto, y la experiencia de construir SafeDriver. ¡Éxitos
en el parcial!*
