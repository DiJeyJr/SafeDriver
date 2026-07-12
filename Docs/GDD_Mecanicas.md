# SafeDriver — Mecánicas y Features (sección para el GDD)

> Documento de diseño consolidado de **todas** las mecánicas y features implementadas en
> SafeDriver. Pensado para anexarse al Documento de Diseño del Juego (GDD). Los valores de
> parámetros son los reales del proyecto al momento de redacción; pueden ajustarse desde el
> Inspector de Unity.
>
> **Proyecto:** simulador VR de escuela de manejo para Meta Quest 3 (Unity 6, URP, Oculus
> Interaction SDK / Meta XR Building Blocks). Idioma de juego: español (Argentina).
> Marco normativo de referencia: **Ley Nacional de Tránsito 24.449**.

---

## 0. Pilares de diseño

SafeDriver no es un juego de manejo arcade: es una **herramienta pedagógica** de educación vial
en VR. Cuatro pilares atraviesan todas las mecánicas:

1. **Filosofía "SafeFail".** El juego **nunca muestra el choque ni el accidente**. Cuando el
   alumno comete una falta grave, la acción se congela y se reemplaza por una **micro-lección**
   que cita el artículo concreto de la Ley 24.449 que regula esa conducta. El error enseña, no
   castiga con violencia.
2. **Aprendizaje por refuerzo doble.** Se **penaliza** la conducta insegura (resta de puntos) y
   se **premia** la conducta segura (suma de puntos + felicitación). El alumno parte de un
   crédito de confianza y lo administra.
3. **Inmersión por controles físicos.** Todo se maneja con **gestos reales** en VR: agarrar y
   girar el volante, mover la palanca de cambios, tirar del freno de mano, accionar el guiño,
   girar la cabeza para mirar los espejos. La UI es **diegética** (pintada sobre el tablero del
   auto, no flotando ante la cara).
4. **Contenido data-driven.** Misiones y niveles son **assets** (ScriptableObjects) que un
   diseñador crea y encadena desde el Inspector sin tocar código. La dificultad del tráfico se
   escala con un perfil editable.

**Arquitectura de integración:** todos los sistemas se comunican por un **EventBus** central
(eventos de velocidad, score, infracción, acción correcta, cambio de estado, fin de nivel). Esto
mantiene desacopladas las capas (Vehículo, Scoring, UI, Audio, Misiones) y permite agregar
features sin tocar los sistemas finalizados.

---

## 1. Bucle de juego y estados

### 1.1 Máquina de estados (GameState)

El juego transita entre cinco estados, cada uno con un efecto sobre la escala de tiempo:

| Estado | Qué es | `Time.timeScale` |
|---|---|---|
| **MainMenu** | Menú principal (pantalla de inicio) | — |
| **Driving** | Conduciendo (estado de juego activo) | 1 (corre) |
| **SafeFail** | Pantalla pedagógica tras falta grave | **sin cambio** (no congela) |
| **LevelEnd** | Resumen / boletín de fin de nivel | 0 (congela) |
| **Paused** | Menú de pausa | 0 (congela) |

El juego arranca en `MainMenu` y pasa a `Driving` al comenzar. Cada transición dispara un evento
al que reaccionan todas las capas desde su propio código (la UI muestra paneles, el vehículo
frena, el timer se pausa). **Decisión clave:** `SafeFail` **no** congela el tiempo (a diferencia
de `LevelEnd`/`Paused`); esto permite el **frenado suave animado** del auto en lugar de un
congelamiento abrupto, coherente con no dramatizar el error.

### 1.2 Condiciones de fin de nivel (fail-states)

El nivel tiene **tres caminos de fracaso** distintos y dos resultados de cierre:

- **SafeFail por infracción grave** → detiene el auto (frenado suave) + pantalla pedagógica. El
  jugador solo puede **Reintentar** o ir al **Menú** (no hay reanudación automática).
- **Tiempo agotado** → cuando el cronómetro llega a `0:00` se dispara *nivel fallido* y aparece
  el panel de fin de nivel.
- **Puntaje en cero** → si el score cae a 0 por acumulación de faltas, se dispara *nivel fallido*.
- **Al terminar normalmente:** **APROBADO** si el puntaje final ≥ `minimumPassScore` (600),
  **REPROBADO** si es menor.

### 1.3 Cronómetro del nivel (LevelTimer)

Cuenta regresiva de **120 s** por defecto, en formato `MM:SS`. Arranca al entrar en `Driving` y
se pausa en SafeFail/Paused/LevelEnd/MainMenu. Cambia de color de forma escalonada para dar
feedback anticipado sin leer los números:

| Tiempo restante | Color |
|---|---|
| > 30 s | Blanco (normal) |
| ≤ 30 s | Amarillo (warning) |
| ≤ 10 s | Rojo (crítico) |

---

## 2. Conducción y vehículo

El vehículo es de **tracción trasera (RWD)**, conducido por físicas reales (WheelColliders). El
centro de masa está bajado para que sea estable y predecible (no vuelca en curvas), de modo que la
dificultad del modelo físico no distraiga del objetivo pedagógico.

### 2.1 Controles de conducción

| Control | Acción | Cómo |
|---|---|---|
| **Volante** | Dirección | Se **agarra** físicamente con la(s) mano(s) y se gira; el aro mapea linealmente a las ruedas delanteras |
| **Gatillo derecho** | Acelerador | Eje analógico (más apretás, más acelera) — torque a ruedas traseras |
| **Gatillo izquierdo** | Freno | Eje analógico — freno a las 4 ruedas |
| **Palanca de cambios** | Marcha D/N/R | Se agarra y se mueve adelante/atrás |
| **Freno de mano** | Retener el auto | Se agarra y se tira hacia arriba |
| **Palanca de guiños** | Luz direccional | Se agarra y se sube/baja (ver §4) |

### 2.2 Volante VR y dirección

El volante se **agarra de verdad** y gira siguiendo la muñeca; la mano virtual queda "pegada" al
aro. Un **filtro de proximidad** evita agarrarlo desde cualquier punto del auto (solo dentro de la
esfera del volante). Mapeo lineal aro → ruedas: el tope del aro (180°) corresponde al ángulo
máximo de las ruedas (32°).

**Recentrado proporcional a la velocidad:** al soltar el volante, vuelve solo al centro **solo si
el auto se mueve**. A baja velocidad el recentrado es lento; a velocidad de crucero o más, rápido.
Parado (0 km/h) el volante se queda donde se dejó. Simula el *self-aligning torque* real y enseña
que el auto se autoendereza al avanzar.

### 2.3 Palanca de cambios D/N/R

Palanca física de tres posiciones (Drive adelante, Neutral medio, Reverse atrás). Al soltarla,
**snapea** al centro de la zona elegida y da un **pulso háptico** al cambiar. **Seguridad de
manejo modelada:**

- **No se cambia de marcha en movimiento:** cuando el auto pasa de parado a andando, la palanca se
  **bloquea** en su zona actual (se estrechan los topes de rotación); al detenerse se libera.
- **No se invierte el sentido sin pasar por Neutral:** un salto D↔R con el auto en movimiento
  fuerza Neutral.

**Modo alternativo sin palanca** (configuración simplificada): estando totalmente detenido,
mantener el **freno apretado 2 s** alterna entre Drive y Reverse; durante esos 2 s el indicador de
marcha parpadea mostrando el destino. Un *latch* exige soltar y re-apretar el freno para evitar
inversiones accidentales.

### 2.4 Freno de mano

Palanca de **dos posiciones** que se agarra y se **tira hacia arriba** (puesto) o se baja
(sacado). Con el freno puesto, el motor se corta (aunque el jugador acelere, no avanza) y se
aplica freno máximo a las 4 ruedas: el auto queda firmemente retenido. Snap a arriba/abajo al
soltar + pulso háptico al enganchar/desenganchar. Enseña a asegurar el auto al estacionar/detener
y a soltarlo antes de arrancar.

### 2.5 Frenado suave en SafeFail (SmoothStop)

Cuando ocurre una falta grave, el auto **no frena de golpe**: desacelera suavemente hasta
detenerse (rampa de ~1.5 s), evitando un frenazo brusco incómodo en VR. Durante la parada se
ignora el input del jugador. Es comfort VR + consecuencia clara sin castigar el cuerpo del alumno.

### 2.6 Tablero diegético

El tablero muestra, sin sacar la vista del parabrisas: **velocímetro analógico** (aguja suavizada,
display hasta 120 km/h), número de velocidad en km/h, puntaje acumulado, cartel de **límite de
velocidad** de la zona, e **indicador de marcha** D (verde) / N (ámbar) / R (naranja) que parpadea
con la marcha de destino durante un cambio.

### 2.7 Parámetros de conducción

| Parámetro | Valor | Nota |
|---|---|---|
| Velocidad máxima adelante | **80 km/h** | Cap urbano |
| Velocidad máxima en reversa | **10 km/h** | Seguridad/realismo |
| Ángulo máximo de ruedas | **32°** | A input ±1 |
| Tope visual del volante | **180°** | Mapea a input ±1 |
| Escala de aceleración | 12 | Torque al motor |
| Escala de freno | 30 | Torque de freno |
| Recentrado del volante | 180 °/s a 30 km/h | Proporcional a la velocidad |
| Offset de centro de masa | (0, −0.4, 0) m | Antivuelco |
| Freno de mano: ángulo puesto / umbral | 40° / 20° | Snap binario |
| Palanca cambios: snap D/R / zona Neutral | ±40° / ±20° | Con histéresis 2° |
| Cambio por freno sostenido | 2 s (detenido) | Modo sin palanca |
| Frenado suave (SmoothStop) | ~1.5 s | Rampa de freno |

---

## 3. Interacción VR (controles físicos)

Todo objeto manipulable usa el **mismo stack de agarre validado** (el del volante, clonado): así
volante, palanca de cambios, freno de mano, palanca de guiños y tablilla del examinador conviven
de forma confiable, cada uno scopeado por su propio filtro de proximidad.

### 3.1 Agarre y manos

- **Seguimiento de la mano:** al agarrar un objeto, la mano virtual se "pega" y lo sigue (no queda
  flotando ni atravesándolo). Funciona con controllers y con manos híbridas.
- **Filtros de proximidad:** solo se puede agarrar un objeto cuando la mano está **físicamente
  sobre él**, no a distancia ("agarre fantasma").

### 3.2 Háptica

| Sistema | Cuándo | Sensación |
|---|---|---|
| **Pulsos por agarre** | Al manipular un objeto agarrable | Vibra el controller que lo sostiene (lado correcto) |
| **Controlador háptico central** | Infracción / acierto (vía EventBus) | Infracción: doble golpe firme. Acierto: rampa ascendente suave |
| **Feedback de límite de agarre** | Forzar un control más allá de su tope | Vibración escalonada por niveles; si se insiste, el objeto **se suelta solo** + sacudón de cámara |

El **sacudón de cámara** está deliberadamente acotado (< 5 cm, ~0.3 s) porque un shake fuerte
marea en VR.

### 3.3 Tablilla del examinador (panel de objetivos)

El **panel de objetivos** vive en una tablilla estilo examinador montada en el tablero. El jugador
puede **agarrarla y moverla libremente (6DOF)** para leerla de cerca; al soltarla, vuelve sola y
suavemente a su lugar de reposo (siempre legible). Refuerza la diégesis de "escuela de manejo":
el examinador lleva su checklist.

---

## 4. Luces direccionales (guiños)

Palanca/stalk que sale de la columna de dirección, a la izquierda del volante. Se **agarra** y se
mueve sobre su eje:

| Posición | Resultado |
|---|---|
| Arriba (> +12°) | Guiño **derecho** |
| Centro (±12°) | Apagado |
| Abajo (< −12°) | Guiño **izquierdo** |

Al soltar, **snapea** y queda enclavada (el guiño sigue activo sin sostenerla, liberando las manos
para el volante). **No** hay auto-cancelación al enderezar el volante (a diferencia de un auto
real). Las luces parpadean en **ámbar a ~1.25 Hz** (arrancan encendidas al accionar). Cada lado
agrupa **3 luces**: esquina delantera + esquina trasera + **indicador en el tablero** frente al
conductor, todas sincronizadas, para que la señal sea visible desde afuera y desde la cabina.

> **Nota de diseño:** actualmente el guiño es una mecánica de **inmersión/realismo**; ningún
> sistema de scoring penaliza no señalizar ni premia señalizar. Es un gancho disponible para
> futuras misiones (ej. "señalizá antes de girar").

---

## 5. Espejos y chequeo de espejos

### 5.1 Espejos funcionales

Los tres espejos (central/retrovisor, lateral izquierdo, lateral derecho) renderizan el **reflejo
real** del mundo detrás del auto (tráfico, peatones, semáforos). Por rendimiento en Quest 3, cada
espejo se actualiza **1 de cada 2 frames** (~36 FPS efectivos), ahorrando ~50% del costo de GPU
sin perder fluidez del tráfico lejano.

### 5.2 Detección del chequeo

El juego registra cuándo el alumno **mira un espejo** y lo premia. Dos detectores coexisten:

- **Por giro de cabeza (Quest 3, principal):** girar la cabeza > 45° a un lado cuenta como mirar
  ese espejo lateral; mirar > 15° hacia arriba cuenta como retrovisor. No requiere eye tracking.
- **Por mirada (Quest Pro, si hay eye tracking):** posar la mirada sobre un espejo (raycast a
  objetos con tag `Mirror`) lo cuenta. Si no hay hardware de eye tracking, queda inactivo y actúa
  el de cabeza.

Cada espejo se detecta una vez por ciclo de maniobra (*rising edge*; se reinicia al iniciar una
nueva maniobra). **Requisito por maniobra:** girar a la izquierda exige el espejo izquierdo, a la
derecha el derecho, **dar marcha atrás exige el retrovisor central**. Cada chequeo otorga **+5
puntos** (acción correcta `CheckedMirrorsBeforeTurn`) y avanza la misión de espejos (requiere 2).

---

## 6. Sistema de evaluación: puntaje, infracciones y aciertos

### 6.1 Puntaje base

El alumno **arranca cada nivel con 1000 puntos** y conduce intentando no perderlos. El puntaje
nunca baja de 0 ni tiene techo. **Aprobado = puntaje final ≥ 600.** Modelo de examen de manejo:
se parte de un crédito de confianza y se pierde con cada error (el manejo seguro es el estado por
defecto; el error es la excepción penalizada).

### 6.2 Tabla de infracciones (penalizaciones)

Cada error resta puntos. Las faltas que ponen vidas en riesgo pesan mucho más que las de hábito.
**Las graves** (★) disparan **SafeFail inmediato** (detención del auto + pantalla pedagógica);
las demás solo restan y la conducción continúa.

| Infracción | Penalización | ¿Grave? | Ley 24.449 |
|---|---:|:---:|---|
| Atropellar a un peatón (`HitPedestrian`) | −30 | ★ SÍ | Art. 41 |
| Choque grave (`SevereCollision`) | −25 | ★ SÍ | Art. 50 |
| Cruzar en rojo (`RanRedLight`) | −20 | ★ SÍ | Art. 43 |
| Circular en contramano (`WrongWay`) | −18 | No | Art. 42 |
| No ceder al peatón (`PedestrianNotYielded`) | −15 | ★ SÍ | Art. 41 |
| No detenerse en PARE (`FailedToStopAtSign`) | −12 | No | Art. 44 |
| Exceso de velocidad (`Speeding`) | −10 | No | Art. 51 |
| Maniobra peligrosa (`DangerousManeuver`) | −8 | No | Art. 48 |
| No chequear espejos (`NoMirrorCheck`) | −5 | No | Art. 39 |

Cada infracción queda registrada con su **timestamp** en el historial del nivel (se lista al
final). Las cuatro graves son: cruzar en rojo, no ceder al peatón, atropello y choque grave.

### 6.3 Tabla de aciertos (bonus por manejo correcto)

El alumno **recupera puntos** haciendo lo correcto. Las acciones de mayor riesgo evitado valen
más, espejando la jerarquía de las penalizaciones:

| Acción correcta | Bonus | `ActionType` |
|---|---:|---|
| Ceder el paso a un peatón | +15 | `YieldedToPedestrian` |
| Detenerse en semáforo rojo | +10 | `StoppedAtRedLight` |
| Detenerse en señal PARE | +8 | `StoppedAtPareSign` |
| Chequear espejos antes de girar | +5 | `CheckedMirrorsBeforeTurn` |
| Cruzar en verde | +3 | `PassedGreenLight` |
| Cruzar senda despejada (sin peatón) | +3 | `PedestrianNotPresent` |
| Mantener velocidad legal | +2 (cada 5 s) | `MaintainedLegalSpeed` |

### 6.4 Pantalla SafeFail (retroalimentación pedagógica)

Es el **núcleo de la propuesta de valor**. Al cometer una falta grave, en vez de un choque o un
"Game Over" genérico, aparece (con fade in suave de 0.5 s) una tarjeta con tres bloques:

1. **Título** corto de la falta (ej. "Semáforo en Rojo").
2. **Descripción** didáctica: qué se hizo mal y cuál era la conducta correcta.
3. **Referencia legal:** el artículo exacto de la Ley 24.449.

Más dos botones: **Reintentar** (recarga el nivel limpio: auto, score, misiones, timer) y **Menú
Principal**. Mapeo infracción → contenido pedagógico:

| Infracción | Título de la pantalla | Artículo |
|---|---|---|
| `RanRedLight` | Semáforo en Rojo | Art. 43 — Señales semafóricas |
| `PedestrianNotYielded` | Prioridad Peatonal | Art. 41 — Prioridad del peatón |
| `HitPedestrian` | Atropellaste a un Peatón | Art. 41 — Prioridad del peatón |
| `FailedToStopAtSign` | Señal de PARE | Art. 44 — Señales de tránsito |
| `Speeding` | Exceso de Velocidad | Art. 51 — Límites de velocidad |
| `NoMirrorCheck` | Espejos No Chequeados | Art. 39 — Requisitos para girar |
| `DangerousManeuver` | Maniobra Peligrosa | Art. 48 — Prohibiciones |
| `WrongWay` | Circulaste en Contramano | Art. 42 — Sentido de circulación |
| `SevereCollision` | Choque Grave | Art. 50 — Velocidad precautoria |

### 6.5 Pantalla de fin de nivel (boletín)

Al terminar, aparece "FIN DEL NIVEL" con: **puntaje final**, veredicto **APROBADO/REPROBADO**, y
la **lista cronológica de infracciones** cometidas con su timestamp `[MM:SS]` y mensaje. Botones
Reintentar y Menú. Cierre tipo boletín de examen: el alumno ve no solo la nota, sino el desglose
de qué hizo mal y cuándo (retroalimentación formativa).

---

## 7. Sensores de detección de manejo

Cada situación de tránsito se evalúa con un **sensor** dedicado. Todos reconocen al jugador por el
tag `PlayerVehicle` (en el collider o su Rigidbody) y despachan premio/infracción al EventBus, que
ScoreManager, háptica, audio y toasts consumen sin conocerse entre sí.

### 7.1 Semáforo (sistema de dos zonas)

Separado deliberadamente en dos componentes para resolver el bug donde aproximarse en rojo
penalizaba y bloqueaba el premio de frenar:

- **Zona de detención (`RedLightStopZone`):** volumen **antes** de la línea (7 m). Si el auto se
  detiene ≥ 0.5 s con el semáforo en rojo → premio **+10**. **Nunca** dispara infracción.
- **Línea de cruce (`TrafficLightCrossLine`):** sobre la línea. Al cruzarla hacia adelante: en
  rojo → infracción **−20 + SafeFail**; en verde → premio **+3**; amarillo configurable (sin
  penalidad por defecto). Retroceder sobre la línea no se evalúa.

La detección de cruce usa un volumen ancho que mide el **cambio de lado** del auto respecto a la
línea, robusto al *tunneling* a alta velocidad, y distingue avance de retroceso.

### 7.2 Señal PARE

El auto debe **detenerse por completo** (≤ 2 km/h) durante **1.5 s** antes de avanzar. Si frena lo
suficiente → premio **+8**; si pasa de largo sin frenar el tiempo requerido → infracción **−12**
(no grave). El umbral exige una detención real, no un *rolling stop*.

### 7.3 Senda peatonal

| Situación | Resultado |
|---|---|
| Hay peatón cruzando y el auto **NO** está detenido al entrar | Infracción `PedestrianNotYielded` **−15 + SafeFail** |
| Hay peatón y el auto se detiene ≥ 0.8 s | Premio `YieldedToPedestrian` **+15** |
| No hay peatón (cruce despejado) | Premio `PedestrianNotPresent` **+3** |
| El auto **toca el cuerpo** del peatón | Infracción `HitPedestrian` **−30 + SafeFail** |

El atropello se detecta como **colisión real con el cuerpo del peatón** (no como evento de zona),
por eso es inequívoco y carga la consecuencia más dura del juego. Prioridad peatonal absoluta
(Art. 41).

### 7.4 Contramano

En ciertos carriles, si el alumno circula en sentido opuesto al permitido (rumbo a más de ~120°
del sentido legal) → infracción `WrongWay` **−18** (no grave). Un umbral generoso evita falsos
positivos en giros, y un mínimo de velocidad evita evaluar rumbo errático estando casi parado.

### 7.5 Límite de velocidad por zona

Cada tramo tiene su límite (40 km/h por defecto), que se muestra en el HUD al entrar. Si el alumno
excede el límite + tolerancia (5 km/h) → infracción `Speeding` **−10** (un disparo por exceso, no
spam). Si mantiene velocidad legal en movimiento → premio `MaintainedLegalSpeed` **+2 cada 5 s**.

### 7.6 Resumen de sensores

| Sensor | Premio | Infracción | Parámetros clave |
|---|---|---|---|
| Zona de rojo | +10 (frenar) | — | detenido ≥ 0.5 s, 7 m antes |
| Línea de semáforo | +3 (verde) | −20 + SafeFail (rojo) | sobre la línea, solo hacia adelante |
| PARE | +8 | −12 | ≤ 2 km/h por 1.5 s |
| Senda peatonal | +15 / +3 | −15 + SafeFail | detenido ≥ 0.8 s |
| Atropello | — | −30 + SafeFail | contacto con el cuerpo |
| Contramano | — | −18 | dot < −0.5, > 3 km/h |
| Velocidad | +2 / 5 s | −10 | límite + 5 km/h |

---

## 8. Integridad del vehículo (sistema de daño)

Puntuación **paralela** al score que modela el desgaste del auto por zona. No toca la física del
vehículo: solo escucha colisiones.

### 8.1 Ocho zonas

El auto tiene 8 zonas independientes, cada una de 0 a 100, que arrancan en 100:

```
        ┌───────────────┐
   FL ● │     FRENTE     │ ● FR        (carrocería: Frente, Atrás,
        │               │              Lateral Izq, Lateral Der)
   IZQ ▌│               │▐ DER         (ruedas: FL, FR, RL, RR
        │               │               en las 4 esquinas)
   RL ● │     ATRÁS      │ ● RR
        └───────────────┘
```

La **integridad total** es el promedio de las 8 zonas, mostrado como porcentaje.

### 8.2 Daño por impacto

El auto se daña al rozar **obstáculos altos** (edificios, postes) y al **chocar contra autos
NPC**. El piso plano y los triggers de misión no dañan. El daño escala con la velocidad del
impacto:

```
daño = velocidad_kmh × 0.8   (acotado entre 6 y 60)
```

Ejemplos: 20 km/h → 16 de daño; 50 km/h → 40; 80 km/h → tope 60. La **zona golpeada** se determina
por el punto de contacto en el espacio local del auto (frente/atrás/laterales, y las esquinas
cuentan como rueda). Un **cooldown de 0.3 s** evita que un roce continuo acumule daño infinito.

### 8.3 SafeFail por daño

Dos condiciones disparan el SafeFail "Choque Grave" (`SevereCollision`, −25):

- **Golpe grave único:** un solo impacto ≥ **35 de daño** (≈ choque a > 44 km/h).
- **Daño acumulado crítico:** la integridad total cae a **≤ 35%**.

Cada una muestra un mensaje pedagógico distinto ("chocaste fuerte" vs "acumuló demasiado daño").

### 8.4 Símbolo visual

Silueta top-down del auto con las 8 zonas que pasan de **verde (100%) a rojo (0%)** + un
porcentaje grande al centro. Se ve **en vivo en el tablero** (en VR) y en la pantalla de fin de
nivel como puntuación de cierre aparte del score.

### 8.5 Parámetros de integridad

| Parámetro | Valor |
|---|---|
| Daño por km/h | 0.8 |
| Daño mínimo / máximo por golpe | 6 / 60 |
| Umbral de golpe grave | 35 |
| Integridad crítica (SafeFail) | 35% |
| Cooldown entre impactos | 0.3 s |

---

## 9. Tráfico AI

### 9.1 Autos NPC

El alumno ve autos circulando solos por la avenida en un **loop oval continuo** (suben por un
carril, bajan de frente por el otro, giran en los extremos). Comportamiento:

- **Movimiento por waypoints**, suave y a velocidad **consistente e independiente del framerate**
  (clave para no marear en VR).
- **Frenado por obstáculo adelante:** un *sphere-cast* detecta al jugador, a otro NPC o a un
  peatón en el camino y frena en vez de embestir; reanuda al despejarse.
- **Frenado ante semáforo en rojo:** desacelera y se detiene antes de la línea; si ya pasó la
  línea, **termina de cruzar** en vez de clavarse en medio de la intersección.
- **Colisión sólida con el jugador:** si el alumno choca un NPC, el impacto es **físico** (no se
  atraviesan). Esto se logra con una **capa de física aislada** ("Traffic") que solo colisiona
  consigo misma: los vehículos chocan entre sí pero ninguno se engancha con calle/edificios.

**Dificultad data-driven (`TrafficProfile`):** un asset define velocidad de crucero, variación de
velocidad por auto, escala de los modelos y cantidad de autos. Subir velocidad/cantidad por nivel
escala la exigencia **sin reabrir la escena**. Valores por defecto: ~14 km/h de crucero, 4 autos.

### 9.2 Peatones

- **Ciclo de cruce:** un peatón espera en la vereda un tiempo variable (2–8 s), baja a la senda y
  la cruza caminando; al terminar vuelve a esperar. Mientras cruza, marca "peatón presente" para
  el detector de cesión.
- **"Mirar antes de cruzar":** el peatón **no baja a la calle mientras haya un vehículo** (el del
  jugador o un NPC) dentro de la zona de peligro sobre el asfalto. Así un atropello **siempre es
  atribuible a una mala decisión del jugador**, nunca a un peatón que se tira bajo las ruedas.
- **Soporte multi-peatón:** varios peatones pueden cruzar a la vez sin que uno, al salir, apague
  el estado de "hay peatones cruzando" mientras otro sigue.

---

## 10. Sistema de misiones (data-driven)

### 10.1 Concepto

Cada misión es un **asset** (ScriptableObject) con título, descripción didáctica opcional, puntos
al completar (10 por defecto) y un flag *opcional*. El jugador **no interactúa directamente** con
las misiones: maneja bien (para en rojo, cede al peatón, chequea espejos…) y esas conductas
correctas **avanzan las misiones automáticamente** vía el vocabulario común de `ActionType`. Un
diseñador crea y tunea objetivos desde el Inspector, sin programar.

### 10.2 Tipos de misión

| Tipo | Qué pide | Ejemplo | Panel |
|---|---|---|---|
| **Contable** | Hacer una acción **N veces** | "Detenerse en rojo ×1", "Chequear espejos ×2" | `1/3` |
| **Secuencia** | Acciones **en orden** | Recorrido A → B → C | paso `1/3` |
| **Con tiempo** | Acción N veces **contrarreloj** | N antes de agotar el reloj | `2/3 · 12s` |
| **Compuesta** | **Todas** las sub-misiones | "Conducción segura en la intersección" = parar en rojo + ceder + chequear | `2/3 subs` |

Diseño tolerante: las misiones de secuencia **ignoran** acciones fuera de orden (no fallan), para
reducir frustración en VR. Las compuestas pueden anidarse.

### 10.3 Estados y orquestación

Cada misión tiene un estado: **Pendiente · En progreso · Cumplida · Fallada**. El `MissionManager`
gestiona el ciclo de vida y avisa cuando **todas las obligatorias** están cumplidas
(`AllRequiredCompleted`) — la condición de victoria del nivel. Las misiones **opcionales** suman
pero no bloquean. Sin misiones obligatorias no hay fin por misiones (modo libre).

---

## 11. Niveles y progresión

### 11.1 Definición de nivel (data-driven)

Cada nivel es un asset con: id, nombre de menú, descripción, **escena** a cargar, **lista de
misiones**, **siguiente nivel** (lo que se desbloquea al completar) y flag de **modo libre**.
Permite crear y **encadenar** niveles sin tocar código.

### 11.2 Flujo de un nivel

1. Al cargar la escena, el panel de objetivos se **puebla** con las misiones del nivel.
2. El alumno maneja; aciertos avanzan misiones, errores restan/disparan SafeFail.
3. Al cumplir todas las misiones obligatorias: se **guarda el mejor puntaje**, se **desbloquea el
   siguiente nivel** (o se marca la campaña como completa) y aparece el boletín de fin de nivel.

### 11.3 Modo libre (free roam)

Modo sandbox sin misiones obligatorias ni condición de fin: el alumno practica manejo seguro
libremente, sumando puntos. Se **habilita al completar la campaña**. Recompensa por terminar los
niveles guiados.

### 11.4 Persistencia

Se recuerda entre sesiones (PlayerPrefs): qué niveles están **desbloqueados**, el **mejor
puntaje** por nivel (solo mejora si se supera) y si la **campaña está completa**. El reset es
por-nivel para no pisar otras preferencias (ej. volumen).

### 11.5 Build Settings

- Índice 0 = **MainMenu** (carga al inicio).
- Índice 1 = **Level_01_Basics**.

El `sceneName` de cada nivel debe estar registrado en Build Settings.

---

## 12. UI, HUD, feedback y tema visual

### 12.1 HUD diegético

Toda la información crítica está **pintada sobre el tablero del auto** (no flota ante la cara):
velocímetro analógico + número de velocidad, puntaje, cartel de límite, indicador de marcha y
panel de objetivos. Mantiene la inmersión VR y enseña a monitorear la velocidad como en un auto
real.

### 12.2 Notificaciones de feedback

| UI | Cuándo | Contenido |
|---|---|---|
| **Success Toast** | Acierto de manejo | "¡Muy bien!" + mensaje + `+N`, se desvanece solo, sobre el volante |
| **Panel de objetivos** | Siempre | Checklist con casilla □ pendiente / ✔ cumplida (tachada) / ✘ fallada |
| **Pantalla SafeFail** | Falta grave | Lección + cita legal (ver §6.4) |
| **Panel fin de nivel** | Fin de nivel | Boletín con score, veredicto e historial (ver §6.5) |

### 12.3 Menús

- **Menú principal:** Jugar (entra al nivel) / Salir.
- **Menú de pausa:** se abre con un botón del control; congela el juego y aparece **siempre frente
  a la mirada** (proyectado al plano horizontal para no quedar inclinado), a distancia de confort.
  Opciones: Reanudar / Reintentar / Menú principal.

### 12.4 Reglas de UI en VR

Todos los canvas son **world-space**, a escala en metros (0.001), parentados a ~0.8–1.2 m de la
cámara (zona de confort). La interacción es por **poke/ray del Oculus Interaction SDK** (los
botones se tocan). Las pantallas que aparecen/desaparecen **desactivan su hitbox** mientras están
ocultas, para que la mano no choque con una "pared invisible" al estirarse hacia el volante.

### 12.5 Tema visual ("manual del conductor + señalética vial")

Look coherente con metáfora de escuela de manejo: fondos crema tipo manual, esquinas redondeadas,
tipografía legible a distancia, y colores de **señalética vial**:

| Color | Rol | Hex |
|---|---|---|
| Verde | Éxito / avanzar | `#3DAE5A` |
| Rojo | Peligro / detener | `#D64550` |
| Ámbar | Precaución / warning | `#F2A93B` |
| Azul | Info / marca | `#2D7DD2` |
| Crema | Fondo base | `#F7F4EC` |

El verde/rojo/ámbar mapean al semáforo y a las señales, anclando el feedback de la UI al código
vial real.

---

## 13. Audio

Todo el audio reacciona al EventBus y se rutea a un mixer master (un solo slider de volumen
controla todo, con curva perceptual y persistencia entre sesiones).

| Sonido | Tipo | Comportamiento |
|---|---|---|
| **Motor** | 3D posicional | Pitch y volumen suben con la velocidad y el acelerador; Doppler bajo (confort VR) |
| **Bocina** | 3D one-shot | Advertir a otros *(API lista; falta wirear el botón)* |
| **Infracción** | 2D one-shot | Sonido de error inmediato y sin ambigüedad |
| **Acierto** | 2D one-shot | Sonido de éxito (refuerzo positivo) |
| **Fin de nivel** | 2D jingle | Jingle de victoria + se silencia el ambiente |
| **Ambiente de ciudad** | 2D loop | Da vida al entorno de fondo |
| **Entidades del mundo** | 3D HRTF | Autos NPC, peatones y semáforos suenan con dirección y distancia reales |

La conciencia situacional auditiva (escuchar de dónde viene el tráfico) es en sí una habilidad de
manejo seguro que el audio 3D entrena.

---

## 14. Apéndice — Marco legal (Ley Nacional de Tránsito 24.449)

Cada conducta evaluada está anclada a un artículo real, que se cita en la pantalla SafeFail y en
los mensajes de los detectores:

| Artículo | Tema | Mecánica asociada |
|---|---|---|
| **Art. 39** | Requisitos para girar | Chequeo de espejos |
| **Art. 41** | Prioridad del peatón | Ceder el paso / atropello |
| **Art. 42** | Sentido de circulación | Contramano |
| **Art. 43** | Señales semafóricas | Semáforo rojo/verde |
| **Art. 44** | Señales de tránsito | Señal PARE |
| **Art. 48** | Prohibiciones al conductor | Maniobra peligrosa |
| **Art. 50** | Velocidad precautoria | Choque grave |
| **Art. 51** | Límites de velocidad | Exceso de velocidad |

---

## 15. Apéndice — Mapa rápido de sistemas

| Sistema | Rol | Integración |
|---|---|---|
| **EventBus** | Bus de eventos central | Punto de integración de todas las capas |
| **GameManager** | Máquina de estados + SafeFail | Despacha cambios de estado |
| **VehicleController** | Conducción (WheelColliders, marchas, freno de mano) | Lee input, escucha estado |
| **ScoreManager** | Puntaje, infracciones, aciertos, aprobación | Escucha detectores por EventBus |
| **VehicleIntegrity** | Daño por zonas → SafeFail | Escucha colisiones, despacha infracción |
| **Detectores (Scoring)** | Semáforo, PARE, senda, contramano, velocidad, espejos | Despachan premio/infracción |
| **MissionManager / LevelManager** | Objetivos y progresión data-driven | Escuchan acciones correctas y score |
| **UIManager** | Ruteo de paneles por estado | Solo escucha eventos |
| **AudioManager** | Sonido reactivo | Solo escucha eventos |
| **Tráfico (Traffic)** | NPCs y peatones | Capa de física aislada |

---

*Documento generado a partir del relevamiento del código fuente del proyecto. Para el detalle de
implementación de cada sistema, ver los scripts en `Assets/_SafeDriver/Scripts/`. Para armar
niveles nuevos, ver `Docs/GuiaLevelDesigner.md`.*
