using System;

namespace SafeDriver.Core
{
    /// <summary>
    /// Bus de eventos estatico del juego.
    /// Desacopla TODOS los sistemas: el publisher no conoce al subscriber.
    /// Cualquier capa puede publicar via Dispatch_* y suscribirse a los events publicos.
    /// </summary>
    public static class EventBus
    {
        // ==========================================================
        //   Eventos de vehiculo
        // ==========================================================
        public static event Action<float> OnSpeedChanged;        // velocidad actual en km/h
        public static event Action<float> OnSteeringChanged;     // angulo normalizado -1..+1
        public static event Action<float> OnSpeedLimitChanged;  // nuevo limite de zona (km/h)

        // ==========================================================
        //   Eventos de infracciones y aciertos
        // ==========================================================
        public static event Action<InfractionType, string> OnInfractionDetected;
        public static event Action<ActionType, int>        OnCorrectActionPerformed;

        // ==========================================================
        //   Eventos de trafico
        // ==========================================================
        public static event Action<UnityEngine.GameObject, LightState> OnTrafficLightChanged;

        // ==========================================================
        //   Eventos de scoring / progresion
        // ==========================================================
        public static event Action<int>   OnScoreChanged;        // puntaje total nuevo
        public static event Action<int>   OnScoreDelta;          // +/- aplicado (para animacion HUD)
        public static event Action        OnLevelComplete;
        public static event Action        OnLevelFailed;

        // ==========================================================
        //   Eventos de caja / marcha
        // ==========================================================
        public static event Action<GearState> OnGearChanged;

        // ==========================================================
        //   Eventos de estado del juego
        // ==========================================================
        public static event Action<GameState, GameState> OnGameStateChanged;  // (previous, current)

        // ==========================================================
        //   Metodos de Dispatch_*  (unica forma de emitir desde fuera)
        // ==========================================================

        // -- Vehiculo --
        public static void Dispatch_SpeedChanged(float speedKmh)  => OnSpeedChanged?.Invoke(speedKmh);
        public static void Dispatch_SteeringChanged(float axis)   => OnSteeringChanged?.Invoke(axis);
        public static void Dispatch_SpeedLimitChanged(float limitKmh) => OnSpeedLimitChanged?.Invoke(limitKmh);

        // -- Infracciones / aciertos --
        public static void Dispatch_Infraction(InfractionType type, string message)
            => OnInfractionDetected?.Invoke(type, message);

        public static void Dispatch_CorrectAction(ActionType type, int bonus)
            => OnCorrectActionPerformed?.Invoke(type, bonus);

        // -- Trafico --
        public static void Dispatch_TrafficLightChanged(UnityEngine.GameObject source, LightState state)
            => OnTrafficLightChanged?.Invoke(source, state);

        // -- Scoring --
        public static void Dispatch_ScoreChanged(int newTotal)    => OnScoreChanged?.Invoke(newTotal);
        public static void Dispatch_ScoreDelta(int delta)         => OnScoreDelta?.Invoke(delta);
        public static void Dispatch_LevelComplete()               => OnLevelComplete?.Invoke();
        public static void Dispatch_LevelFailed()                 => OnLevelFailed?.Invoke();

        // -- Marcha --
        public static void Dispatch_GearChanged(GearState gear) => OnGearChanged?.Invoke(gear);

        // -- Estado del juego --
        public static void Dispatch_GameStateChanged(GameState previous, GameState current)
            => OnGameStateChanged?.Invoke(previous, current);

        // ==========================================================
        //   Utilidad: limpiar todos los subscribers al cambiar escena
        // ==========================================================
        public static void Clear()
        {
            OnSpeedChanged = null;
            OnSteeringChanged = null;
            OnSpeedLimitChanged = null;
            OnInfractionDetected = null;
            OnCorrectActionPerformed = null;
            OnScoreChanged = null;
            OnScoreDelta = null;
            OnLevelComplete = null;
            OnLevelFailed = null;
            OnTrafficLightChanged = null;
            OnGearChanged = null;
            OnGameStateChanged = null;
        }
    }
}
