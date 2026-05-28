# Cómo crear niveles

Un nivel en SafeDriver = una **escena** + un **LevelDefinition** (asset que dice
qué misiones tiene y con qué nivel encadena) + dos managers en la escena. Esta
guía arma un nivel nuevo de cero.

Antes de empezar, conviene tener las misiones listas: ver
[CrearMisiones.md](CrearMisiones.md).

---

## Anatomía de un nivel

```
Escena (ej. Level_02_Avenida.unity)
├─ Player rig (Meta XR Building Blocks)        ← el auto + cámara VR
├─ Detectores en el mundo                       ← semáforos, sendas, señales PARE
│   ├─ TrafficLightDetector (+ TrafficLightController)
│   ├─ CrosswalkDetector    (+ peatón)
│   └─ StopSignDetector
├─ MissionSystem (GameObject)
│   ├─ MissionManager   ← orquesta las misiones
│   └─ LevelManager     ← carga el LevelDefinition, maneja fin de nivel
└─ UI diegética (panel de objetivos en el tablero, HUD)

       +  asset  Level_02_Avenida.asset (LevelDefinition)
```

El `LevelManager` lee el `LevelDefinition`, le pasa las misiones al
`MissionManager`, y cuando todas las obligatorias se completan dispara el fin de
nivel (desbloquea el siguiente y muestra el panel de resumen).

---

## 1. Crear el LevelDefinition

1. En **Project**, click derecho → **Create → SafeDriver → Nivel**.
2. Nombralo (ej. `Level_02_Avenida`). Guardalo en
   `Assets/_SafeDriver/Missions/Levels/`.
3. Completá el Inspector:

   | Campo | Qué poner | Ejemplo |
   |-------|-----------|---------|
   | Level Id | Id único (clave de guardado) | `level_02_avenida` |
   | Display Name | Nombre en el menú de selección | `Avenida - Intermedio` |
   | Description | Resumen del nivel | `Manejá por la avenida...` |
   | Scene Name | Nombre EXACTO de la escena (sin `.unity`) | `Level_02_Avenida` |
   | Missions | Arrastrá los assets de misión del nivel | (varios) |
   | Next Level | El `LevelDefinition` que se desbloquea al pasar | `Level_03_...` o vacío |
   | Is Free Roam | Marcado solo en el nivel de modo libre | desmarcado |

> 📷 _(screenshot: Inspector de un LevelDefinition con misiones asignadas)_

> ⚠️ **Scene Name** tiene que coincidir exactamente con el nombre de la escena y
> esa escena tiene que estar en **Build Settings** (ver paso 4).

---

## 2. Armar la escena

La forma más rápida y segura es **duplicar una escena que ya funciona**
(`Level_01_City`) y cambiarle el contenido, porque ya trae el player rig VR
configurado con Building Blocks.

1. Duplicá `Assets/_SafeDriver/Scenes/Level_01_City.unity` y renombrala.
2. Modelá el mundo (calles, edificios) como quieras.
3. Colocá los detectores donde correspondan:
   - **Semáforo**: un `TrafficLightController` + un GameObject hijo con
     `TrafficLightDetector` y un BoxCollider (Is Trigger ✓) sobre la línea de
     detención. Asigná el `TrafficLightController` al campo del detector.
   - **Senda peatonal**: un GameObject con `PedestrianCrossingDetector` +
     BoxCollider trigger, y un peatón (`NPCPedestrianAI` o `DemoPedestrianFaker`)
     con el detector asignado en `crossingNotifierRef`.
   - **Señal PARE**: `StopSignDetector` + trigger.
4. El auto del jugador debe tener el tag **`PlayerVehicle`** (los detectores lo
   buscan por ese tag).

> 📷 _(screenshot: jerarquía de la escena mostrando los detectores)_

---

## 3. Montar los managers (MissionSystem)

Tenés dos opciones:

**Opción A — automática (recomendada para Level_01_City):**
está el editor utility `SafeDriver/Setup Mission System (Level_01_City)` que crea
todo. Para un nivel nuevo, copiá ese script y adaptá los ids/misiones, o usá la
opción manual.

**Opción B — manual:**
1. Creá un GameObject vacío llamado `MissionSystem`.
2. Agregale los componentes **MissionManager** y **LevelManager**.
3. En **MissionManager**, desmarcá `Auto Load Inspector Missions` (el LevelManager
   inyecta las misiones; si lo dejás marcado, se cargarían dos veces).
4. En **LevelManager**, asigná el `LevelDefinition` de este nivel al campo `Level`.

```
MissionSystem
├─ MissionManager   (Auto Load Inspector Missions = OFF)
└─ LevelManager     (Level = Level_02_Avenida.asset)
```

> 📷 _(screenshot: GameObject MissionSystem con los dos componentes)_

---

## 4. Agregar la escena a Build Settings

1. **File → Build Settings** (o **Build Profiles** en Unity 6).
2. Con la escena abierta, **Add Open Scenes**.
3. Verificá el orden: `MainMenu` debe quedar en índice 0.

> Si el nivel no está en Build Settings, `SceneManager.LoadScene` no lo encuentra
> y el cambio de nivel falla.

---

## 5. Encadenar niveles (progresión)

- En cada `LevelDefinition`, el campo **Next Level** apunta al siguiente.
- Al completar todas las misiones obligatorias, `LevelManager` **desbloquea** el
  Next Level (guardado en `LevelProgress`, vía PlayerPrefs) y dispara el panel de
  fin de nivel.
- Si **Next Level** está vacío, ese nivel es el último de la campaña: al pasarlo
  se marca la campaña como completa (lo que habilita el modo libre).

El botón "Siguiente nivel" del panel de fin llama a `LevelManager.LoadNextLevel()`.
El de "Reintentar" llama a `ReloadLevel()`.

---

## 6. Modo libre

El modo libre es un `LevelDefinition` especial con **Is Free Roam = ✓**:

- No carga misiones obligatorias, así que nunca dispara "fin de nivel".
- Sirve para manejar libre por la ciudad sumando puntos por conducción correcta.
- Conviene habilitarlo en el menú solo cuando `LevelProgress.IsCampaignComplete`
  sea verdadero (todos los niveles pasados).

---

## 7. Progresión guardada (LevelProgress)

`LevelProgress` (estático, usa PlayerPrefs) guarda:

- **Desbloqueo** por `levelId` (`IsUnlocked` / `Unlock`).
- **Mejor puntaje** por nivel (`GetBestScore` / `ReportScore`).
- **Campaña completa** (`IsCampaignComplete` / `MarkCampaignComplete`).

Para resetear el progreso de un nivel durante pruebas:
`LevelProgress.ResetLevel("level_02_avenida")`.

---

## Checklist para un nivel nuevo

- [ ] Misiones creadas (ver [CrearMisiones.md](CrearMisiones.md))
- [ ] `LevelDefinition` creado con Scene Name correcto y misiones asignadas
- [ ] Escena armada (duplicada de una que funcione, con player rig VR)
- [ ] Auto con tag `PlayerVehicle`
- [ ] Detectores colocados y referenciados
- [ ] `MissionSystem` con MissionManager (auto-load OFF) + LevelManager (Level asignado)
- [ ] Escena en Build Settings
- [ ] `Next Level` encadenado (o vacío si es el último)
- [ ] Probado en VR: las misiones se tildan, el nivel termina al completarlas

---

Anterior: [Cómo crear misiones](CrearMisiones.md).
