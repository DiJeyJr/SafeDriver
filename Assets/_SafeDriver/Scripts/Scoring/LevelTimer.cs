using TMPro;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Countdown del nivel. Arranca al entrar a GameState.Driving y se pausa en SafeFail/Paused/LevelEnd
    /// (ademas de la pausa natural por TimeScale=0 que setea GameManager). Cuando llega a 0 dispara
    /// EventBus.Dispatch_LevelFailed() — UIManager mostrara el LevelEndPanel.
    ///
    /// El display es opcional: si se asigna un TextMeshPro 3D se actualiza con formato MM:SS y cambia
    /// de color cuando queda poco tiempo. Otros sistemas pueden leer LevelTimer.Instance.RemainingSeconds.
    /// </summary>
    public class LevelTimer : MonoBehaviour
    {
        public static LevelTimer Instance { get; private set; }

        [Header("Configuracion")]
        [Tooltip("Duracion total del nivel en segundos.")]
        [SerializeField] private float durationSeconds = 120f;

        [Tooltip("Arranca automaticamente al entrar al estado Driving.")]
        [SerializeField] private bool startOnDriving = true;

        [Header("Display (TMP 3D, opcional)")]
        [SerializeField] private TextMeshPro display;

        [Tooltip("Color normal del timer.")]
        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("Color cuando quedan <= warningSeconds.")]
        [SerializeField] private Color warningColor = new Color(1f, 0.85f, 0.4f);

        [Tooltip("Color cuando quedan <= criticalSeconds.")]
        [SerializeField] private Color criticalColor = new Color(1f, 0.3f, 0.3f);

        [SerializeField] private float warningSeconds = 30f;
        [SerializeField] private float criticalSeconds = 10f;

        public float RemainingSeconds { get; private set; }
        public bool IsRunning { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            RemainingSeconds = durationSeconds;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            EventBus.OnGameStateChanged += HandleStateChanged;
            UpdateDisplay();
        }

        void OnDisable()
        {
            EventBus.OnGameStateChanged -= HandleStateChanged;
        }

        void Update()
        {
            if (!IsRunning) return;

            RemainingSeconds -= Time.deltaTime;
            if (RemainingSeconds <= 0f)
            {
                RemainingSeconds = 0f;
                IsRunning = false;
                UpdateDisplay();
                EventBus.Dispatch_LevelFailed();
                return;
            }
            UpdateDisplay();
        }

        private void HandleStateChanged(GameState previous, GameState current)
        {
            switch (current)
            {
                case GameState.Driving:
                    if (startOnDriving && RemainingSeconds > 0f) IsRunning = true;
                    break;
                case GameState.SafeFail:
                case GameState.Paused:
                case GameState.LevelEnd:
                case GameState.MainMenu:
                    IsRunning = false;
                    break;
            }
        }

        private void UpdateDisplay()
        {
            if (display == null) return;

            int total = Mathf.CeilToInt(RemainingSeconds);
            int min = total / 60;
            int sec = total % 60;
            display.text = string.Format("{0:00}:{1:00}", min, sec);

            if (RemainingSeconds <= criticalSeconds) display.color = criticalColor;
            else if (RemainingSeconds <= warningSeconds) display.color = warningColor;
            else display.color = normalColor;
        }

        /// <summary>Reinicia el timer al duration original. Util para reintentar nivel.</summary>
        public void ResetTimer()
        {
            RemainingSeconds = durationSeconds;
            IsRunning = false;
            UpdateDisplay();
        }
    }
}
