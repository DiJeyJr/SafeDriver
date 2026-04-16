using UnityEngine;

namespace SafeDriver.Core
{
    /// <summary>
    /// Singleton orquestador del flujo del juego.
    /// Cambia de estado via TransitionTo() y publica OnGameStateChanged por el EventBus.
    /// No referencia directamente a capas superiores (Vehicle, UI, etc.) — esas capas
    /// se suscriben al evento y reaccionan desde su propio codigo.
    ///
    /// NOTA: la decision de cuando ir a SafeFail la toma ScoreManager (infraccion grave)
    /// o cualquier otro sistema que llame TransitionTo(SafeFail). GameManager solo ejecuta
    /// la transicion y publica el evento.
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
            // Cachea el ultimo mensaje de infraccion (para que UIManager pueda mostrarlo en SafeFail)
            EventBus.OnInfractionDetected += CacheInfractionMessage;
        }

        void OnDisable()
        {
            EventBus.OnInfractionDetected -= CacheInfractionMessage;
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

            switch (newState)
            {
                case GameState.Driving:  OnEnterDriving(previous);  break;
                case GameState.SafeFail: OnEnterSafeFail(previous); break;
                case GameState.LevelEnd: OnEnterLevelEnd(previous); break;
                case GameState.Paused:   OnEnterPaused(previous);   break;
                case GameState.MainMenu: OnEnterMainMenu(previous); break;
            }

            EventBus.Dispatch_GameStateChanged(previous, newState);
        }

        private void CacheInfractionMessage(InfractionType type, string message)
        {
            LastInfractionMessage = message;
        }

        // ---------- Hooks locales ----------

        private void OnEnterDriving(GameState previous)
        {
            Time.timeScale = 1f;
        }

        private void OnEnterSafeFail(GameState previous)
        {
            Debug.Log("[GameManager] SafeFail: " + LastInfractionMessage);
        }

        private void OnEnterLevelEnd(GameState previous)
        {
            Time.timeScale = 0f;
        }

        private void OnEnterPaused(GameState previous)
        {
            Time.timeScale = 0f;
        }

        private void OnEnterMainMenu(GameState previous) { }
    }
}
