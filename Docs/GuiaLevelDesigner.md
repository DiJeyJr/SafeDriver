# Guía del Level Designer — armar un nivel de cero

Esta es **la guía principal** para diseñar niveles en SafeDriver **sin tocar código**.
Todo se hace arrastrando **prefabs** y configurando **assets** desde el Inspector.

> Si querés el detalle fino de los tipos de misión, ver
> [CrearMisiones.md](CrearMisiones.md). Para la teoría de progresión/escenas,
> [CrearNiveles.md](CrearNiveles.md). **Esta guía las resume y las conecta.**

---

## 🎯 El flujo en 6 pasos (TL;DR)

1. **Creá las misiones** del nivel (assets) → *Create → SafeDriver → Misiones*.
2. **Duplicá** una escena que ya funcione (`Level_01_City`) y renombrala.
3. **Arrastrá los prefabs de zona** (semáforo, PARE, senda, etc.) donde van. Ya vienen armados.
4. **Creá el LevelDefinition** + montá el **MissionSystem** (2 componentes).
5. **Agregá la escena a Build Settings** y encadenala con el siguiente nivel.
6. **Probá** en VR.

Cada paso está detallado abajo. La idea central: **arrastrás un prefab y se conecta
solo con la misión** que tenga el `ActionType` correspondiente. No hay que wirear nada
entre el prefab y la misión.

---

## 🧩 Concepto clave: cómo se conecta todo

```
   Prefab de zona en la escena          Misión (asset)
   (ej. MissionZone_TrafficLight)       (ej. "Frenar en rojo", Contable)
            │                                   ▲
            │ el auto frena en rojo             │ cuenta StoppedAtRedLight
            ▼                                   │
        dispara ActionType.StoppedAtRedLight ───┘   (acoplamiento por EventBus)
            │
            └─► el panel de objetivos del tablero se tilda
```

**No hay referencia directa** entre el prefab y la misión. Se enganchan por el
**`ActionType`**: si un prefab dispara `StoppedAtRedLight` y existe una misión que
cuenta `StoppedAtRedLight`, esa misión avanza. Por eso podés poner 3 semáforos y una
sola misión "frenar en rojo 1 vez": el primero que frenes la completa.

---

## 🚦 Los prefabs de zona (lo que arrastrás)

Están en `Assets/_SafeDriver/Prefabs/MissionZones/`. **Todos son autocontenidos**:
los arrastrás a la escena, los rotás hacia la calle y listo.

> **Convención de orientación:** el **+Z local** de cada prefab = **sentido de avance
> legal**. Rotá el prefab (eje Y) para que su "adelante" apunte hacia donde vienen los
> autos. La flecha azul del Move Tool es el +Z.

> _(captura: los 6 prefabs en fila — ver `img/mission-zones-overview.png`)_

| Prefab | Qué hace | Dispara (ActionType / Infracción) | Hay que wirear |
|--------|----------|-----------------------------------|----------------|
| **MissionZone_TrafficLight** | Semáforo completo: trae su propio `TrafficLightController` + las zonas de premio (frenar en rojo) y de cruce (rojo=infracción / verde=premio) | `StoppedAtRedLight`, `PassedGreenLight` / `RanRedLight` | **Nada.** Ya viene wireado |
| **MissionZone_StopSign** | Señal de PARE con su línea de detención | `StoppedAtPareSign` / `FailedToStopAtSign` | Nada |
| **MissionZone_SpeedLimit** | Zona con límite de velocidad (40 por defecto) | `MaintainedLegalSpeed` / `Speeding` | Cambiar el límite en el componente `SpeedLimitZone` si querés otro número |
| **MissionZone_Direction** | Zona de contramano (sentido obligatorio) | — / `WrongWay` | Nada. Rotá el prefab al sentido legal |
| **MissionZone_Crosswalk** | Senda peatonal con su detector | `YieldedToPedestrian`, `PedestrianNotPresent` / `PedestrianNotYielded` | Para que premie "ceder paso", wirearle un peatón (ver abajo) |
| **Pedestrian_Crossing** | Un peatón (con hitbox de atropello) | — / `HitPedestrian` | Para que camine, agregarle un path (ver abajo) |

### Cómo colocar un semáforo (el más usado)
1. Arrastrá `MissionZone_TrafficLight` a la escena.
2. Movelo sobre la línea de detención del carril.
3. Rotalo en Y para que el **+Z mire hacia donde avanzan los autos**.
4. Listo: los focos ciclan solos (verde→amarillo→rojo) y las zonas ya están wireadas
   a su controller interno. Frenar en rojo premia; cruzar en rojo es infracción grave.

### Cómo colocar una senda con peatón que cede el paso
1. Arrastrá `MissionZone_Crosswalk` sobre la senda.
2. Arrastrá `Pedestrian_Crossing` a un costado (la vereda).
3. En el **`PedestrianCrossingDetector`** de la senda, asigná el peatón al campo
   notificador (o usá `DemoPedestrianFaker` con dos waypoints, ver `Level_01_City`
   como ejemplo: `CrosswalkDetector` + `PedestrianFaker` + `WaypointA/B`).
4. **Bonus (recomendado):** poné un `CrosswalkTrafficGate` sobre el asfalto y
   asignáselo al peatón → así el peatón **espera** si viene un auto y no se manda a
   cruzar (evita el "no cediste el paso" injusto). Ver `Level_01_City`.

---

## 🚗 Paso 1 — Crear las misiones del nivel

1. En **Project**, andá a la carpeta del nivel (ej. `Assets/_SafeDriver/Missions/MiNivel/`).
2. Click derecho → **Create → SafeDriver → Misiones → Contable** (el tipo más común).
3. Nombrá el asset (ej. `m_rojo`) y completá el Inspector:

   | Campo | Qué poner |
   |-------|-----------|
   | Mission Id | id único sin espacios (`m_rojo`) |
   | Title | lo que ve el jugador (`Detenerse en semáforo rojo`) |
   | Points | puntos al completar (`10`) |
   | Is Optional | desmarcado = obligatoria para pasar el nivel |
   | **Action** | la acción que cuenta (`Stopped At Red Light`) |
   | Required Count | cuántas veces (`1`) |

> Tipos de misión (Contable / Secuencia / Con tiempo / Compuesta) y la lista completa
> de `ActionType`: ver [CrearMisiones.md](CrearMisiones.md).
> _(captura: `img/mision-contable-inspector.png`)_

**Regla de oro:** el `Action` de la misión tiene que coincidir con lo que dispara el
prefab. Si ponés un `MissionZone_StopSign` querés una misión con `Action = Stopped At
Pare Sign`.

---

## 🏙️ Paso 2 — Armar la escena

La forma **más segura**: duplicar `Level_01_City` (ya trae el rig VR con Building Blocks,
el auto con tag `PlayerVehicle`, el tablero, etc.).

1. En Project, seleccioná `Assets/_SafeDriver/Scenes/Level_01_City.unity` → **Ctrl+D**.
2. Renombrala (ej. `Level_02_Avenida.unity`).
3. Abrila. Borrá/movés los prefabs de zona viejos y armás tu mundo.

> ⚠️ El auto del jugador **debe** tener el tag `PlayerVehicle` (los prefabs lo detectan
> por ese tag). Si duplicaste de `Level_01_City` ya lo tiene.

---

## 🧱 Paso 3 — Arrastrar los prefabs de zona

Arrastrá desde `Assets/_SafeDriver/Prefabs/MissionZones/` los que necesite el nivel,
posicionalos sobre la calle y **rotalos al sentido de avance** (+Z local).

- Semáforo → sobre la línea de detención.
- PARE → en la esquina, con la línea sobre el cruce.
- Límite de velocidad → al inicio del tramo (la zona cubre el tramo siguiente).
- Senda + peatón → sobre la cebra.
- Contramano → en el carril de sentido único.

> _(captura: `img/escena-prefabs-colocados.png`)_

No hace falta tocar nada más en los prefabs (salvo el límite de velocidad si querés
otro número, o wirear el peatón a la senda).

---

## 🎛️ Paso 4 — Crear el LevelDefinition y montar el MissionSystem

### 4a. El LevelDefinition (el "índice" del nivel)
1. **Create → SafeDriver → Nivel**. Guardalo en `Assets/_SafeDriver/Missions/Levels/`.
2. Completá:

   | Campo | Qué poner |
   |-------|-----------|
   | Level Id | id único (`level_02_avenida`) |
   | Display Name | nombre en el menú (`Avenida - Intermedio`) |
   | Scene Name | nombre EXACTO de la escena, sin `.unity` (`Level_02_Avenida`) |
   | **Missions** | arrastrá los assets de misión que creaste en el paso 1 |
   | Next Level | el `LevelDefinition` del siguiente nivel (o vacío si es el último) |
   | Is Free Roam | desmarcado (salvo el nivel de modo libre) |

> _(captura: `img/leveldefinition-inspector.png`)_

### 4b. El MissionSystem (los managers en la escena)
1. Creá un GameObject vacío llamado **`MissionSystem`**.
2. Agregale **MissionManager** y **LevelManager**.
3. En **MissionManager**: desmarcá `Auto Load Inspector Missions` (el LevelManager
   inyecta las misiones; si lo dejás, se cargan dos veces).
4. En **LevelManager**: asigná tu `LevelDefinition` al campo **Level**.

```
MissionSystem
├─ MissionManager   (Auto Load Inspector Missions = OFF)
└─ LevelManager     (Level = Level_02_Avenida.asset)
```

> _(captura: `img/missionsystem-componentes.png`)_

> 💡 Si duplicaste `Level_01_City`, el `MissionSystem` ya está: solo cambiá el `Level`
> del `LevelManager` por tu nuevo `LevelDefinition`.

---

## 📦 Paso 5 — Build Settings y encadenar

1. **File → Build Settings** (o **Build Profiles** en Unity 6) → con la escena abierta,
   **Add Open Scenes**.
2. Verificá que `MainMenu` quede en índice **0**.
3. **Encadenar:** en cada `LevelDefinition`, el campo **Next Level** apunta al siguiente.
   Al completar las misiones obligatorias, el `LevelManager` desbloquea ese Next Level
   y muestra el panel de fin de nivel. Si Next Level está vacío, es el último (se marca
   la campaña como completa → habilita el modo libre).

> ⚠️ Si la escena no está en Build Settings, el cambio de nivel **falla**
> (`SceneManager.LoadScene` no la encuentra).

---

## 🚙 Tráfico NPC (opcional pero recomendado)

Para llenar el nivel de autos que circulan, respetan el semáforo, frenan ante el peatón
y no te embisten:

- En `Level_01_City` está el menú **`SafeDriver/Setup City Traffic`** que monta un loop
  de autos sobre la avenida. Para tu nivel, ajustá las coordenadas del path en
  `Assets/_SafeDriver/Scripts/Editor/SetupCityTraffic.cs` (o copialo y adaptalo).
- La **velocidad / tamaño / cantidad** se configuran en el asset
  `Assets/_SafeDriver/Config/DefaultTrafficProfile.asset` (un `TrafficProfile`).
  **Subí la velocidad por nivel para escalar la dificultad.**

---

## ✅ Checklist de un nivel nuevo

- [ ] Misiones creadas (assets) con el `Action` correcto
- [ ] Escena duplicada de una que funcione (auto con tag `PlayerVehicle`)
- [ ] Prefabs de zona arrastrados, posicionados y **rotados al sentido de avance**
- [ ] Peatón wireado a la senda (si hay misión de "ceder paso")
- [ ] `LevelDefinition` con Scene Name correcto + misiones asignadas
- [ ] `MissionSystem`: MissionManager (auto-load OFF) + LevelManager (Level asignado)
- [ ] Escena en Build Settings
- [ ] `Next Level` encadenado (o vacío si es el último)
- [ ] (Opcional) Tráfico NPC montado y `TrafficProfile` ajustado
- [ ] Probado en VR: las misiones se tildan y el nivel termina al completarlas

---

## 🧪 Probar rápido sin armar todo

1. Abrí `Level_01_City` (ya tiene todo montado).
2. Seleccioná `MissionSystem` → `LevelManager` → su `Level` (el LevelDefinition).
3. Abrí ese asset y agregá/quitá misiones del array **Missions**.
4. **Play** → el panel de objetivos del tablero refleja las misiones cargadas.

---

## 📸 Sobre las capturas

Los marcadores `_(captura: img/...)_` señalan dónde va una imagen del editor (Inspector,
jerarquía). Para completarlas: sacá el screenshot desde Unity, guardalo en `Docs/img/`
con ese nombre, y reemplazá la línea del marcador por `![desc](img/nombre.png)`.
La guía es completa sin las imágenes; son un complemento.
