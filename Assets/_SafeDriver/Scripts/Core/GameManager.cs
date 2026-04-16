using UnityEngine;

namespace SafeDriver.Core
{
    /// <summary>
    /// Singleton orquestador del flujo del juego.
    /// Cambia de estado via TransitionTo() y publica OnGameStateChanged por el EventBus.
    /// No referencia directamente a capas superiores (Vehicle, UI, etc.) — esas capas
    /// se suscriben al evento y reaccionan desde su propio codigo.
    ///
    /// Tambien cachea el ultimo InfractionMessage para que la capa UI pueda leerlo al
    /// entrar en SafeFail y mostrar el texto pedagogico correspondiente.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public GameState CurrentState { get; private set; }

        /// <summary>Ultimo mensaje de infraccion. La UI lo lee al entrar a SafeFail.</summary>
        public string LastInfractionMessage { get; private set; } = string.Empty;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            CurrentState = GameState.MainMenu;
        }

        void OnEnable()
        {
            EventBus.OnInfractionDetected += HandleInfraction;
        }

        void OnDisable()
        {
            EventBus.OnInfractionDetected -= HandleInfraction;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            TransitionTo(GameState.Driving);
        }

        /// <summary>
        /// Cambia el estado del juego y dispara OnGameStateChanged.
        /// Cada capa (Vehicle, UI, Audio...) decide que hacer al recibir el evento.
        /// </summary>
        public void TransitionTo(GameState newState)
        {
            if (CurrentState == newState) return;

            var previous = CurrentState;
            CurrentState = newState;

            // Hook local opcional — util para logging/debug/telemetry sin
            // hardcodear dependencias a otras capas.
            switch (newState)
            {
                case GameState.Driving:  OnEnterDriving(previous);  break;
                case GameState.SafeFail: OnEnterSafeFail(previous); break;
                case GameState.LevelEnd: OnEnterLevelEnd(previous); break;
                case GameState.Paused:   OnEnterPaused(previous);   break;
                case GameState.MainMenu: OnEnterMainMenu(previous); break;
            }

            // Publicar al bus para que capas superiores (Vehicle/UI/Audio) reaccionen.
            EventBus.Dispatch_GameStateChanged(previous, newState);
        }

        private void HandleInfraction(InfractionType type, string message)
        {
            LastInfractionMessage = message;
            // Politica actual: una infraccion dispara SafeFail.
            // Si se quiere cambiar a "X infracciones acumuladas -> SafeFail" se ajusta aqui.
            TransitionTo(GameState.SafeFail);
        }

        // ---------- Hooks locales (solo logging / telemetry, NO refs a otras capas) ----------

        private void OnEnterDriving(GameState previous)
        {
            // p.e. Time.timeScale = 1f; (pero si Paused ya lo hizo, lo deshace aca)
            Time.timeScale = 1f;
        }

        private void OnEnterSafeFail(GameState previous)
        {
            // Detener vehiculo suavemente -> lo hace VehicleController suscrito al evento.
            // Mostrar pantalla pedagogica      -> lo hace UIManager suscrito al evento.
            // Aqui solo logging / analytics.
            Debug.Log($"[GameManager] SafeFail: {LastInfractionMessage}");
        }

        private void OnEnterLevelEnd(GameState previous)
        {
            // Detener el juego cuando se muestra resumen.
            Time.timeScale = 0f;
        }

        private void OnEnterPaused(GameState previous)
        {
            Time.timeScale = 0f;
        }

        private void OnEnterMainMenu(GameState previous) { }
    }
}
