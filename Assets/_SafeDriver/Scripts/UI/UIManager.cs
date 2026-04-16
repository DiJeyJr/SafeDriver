using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Orquestador de paneles UI. Escucha OnGameStateChanged y muestra la pantalla apropiada.
    /// No referencia a Vehicle ni a Scoring directamente — solo reacciona a los eventos del bus.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private HUDController hud;
        [SerializeField] private SafeFailScreen safeFailScreen;
        [SerializeField] private LevelEndPanel levelEndPanel;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            EventBus.OnGameStateChanged += HandleGameStateChanged;
            EventBus.OnLevelComplete    += HandleLevelComplete;
            EventBus.OnLevelFailed      += HandleLevelFailed;
        }

        void OnDisable()
        {
            EventBus.OnGameStateChanged -= HandleGameStateChanged;
            EventBus.OnLevelComplete    -= HandleLevelComplete;
            EventBus.OnLevelFailed      -= HandleLevelFailed;
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            switch (current)
            {
                case GameState.Driving:
                    HideAll();
                    if (hud != null) hud.gameObject.SetActive(true);
                    break;

                case GameState.SafeFail:
                    if (GameManager.Instance != null)
                        ShowSafeFailScreen(GameManager.Instance.LastInfractionType);
                    break;

                case GameState.LevelEnd:
                    if (levelEndPanel != null) levelEndPanel.gameObject.SetActive(true);
                    break;

                case GameState.Paused:
                    // TODO: pause panel
                    break;

                case GameState.MainMenu:
                    HideAll();
                    break;
            }
        }

        public void ShowSafeFailScreen(InfractionType type)
        {
            if (safeFailScreen != null) safeFailScreen.Show(type);
        }

        private void HandleLevelComplete() { /* TODO: mostrar LevelEndPanel con exito */ }
        private void HandleLevelFailed()   { /* TODO: mostrar LevelEndPanel con fallo */ }

        private void HideAll()
        {
            if (safeFailScreen != null) safeFailScreen.Hide();
            // TODO: hide otros paneles
        }
    }
}
