using UnityEngine;
using UnityEngine.SceneManagement;
using SafeDriver.Core;

namespace SafeDriver.Missions
{
    /// <summary>
    /// Coordina un nivel en runtime: inyecta sus misiones al MissionManager, escucha
    /// cuando se completan las obligatorias, guarda progreso/score y desbloquea el
    /// siguiente nivel. Va en cada escena de nivel con su LevelDefinition asignada.
    ///
    /// Corre antes que MissionManager (execution order menor) para inyectar las
    /// misiones a tiempo. Lee el score via EventBus.OnScoreChanged para no acoplar
    /// a SafeDriver.Scoring.
    /// </summary>
    [DefaultExecutionOrder(-60)]
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        [Header("Nivel")]
        [Tooltip("Definicion de este nivel (misiones, escena, siguiente nivel).")]
        [SerializeField] private LevelDefinition level;

        public LevelDefinition Level => level;

        private int lastScore;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()  => EventBus.OnScoreChanged += HandleScoreChanged;
        void OnDisable()
        {
            EventBus.OnScoreChanged -= HandleScoreChanged;
            if (MissionManager.Instance != null)
                MissionManager.Instance.AllRequiredCompleted -= HandleAllRequiredCompleted;
        }

        void Start()
        {
            if (level == null)
            {
                Debug.LogWarning("[LevelManager] Sin LevelDefinition asignada.", this);
                return;
            }

            if (MissionManager.Instance != null)
            {
                if (!level.isFreeRoam && level.missions != null)
                    MissionManager.Instance.LoadMissions(level.missions);

                MissionManager.Instance.AllRequiredCompleted += HandleAllRequiredCompleted;
            }
        }

        private void HandleScoreChanged(int score) => lastScore = score;

        private void HandleAllRequiredCompleted()
        {
            LevelProgress.ReportScore(level.levelId, lastScore);

            if (level.nextLevel != null)
                LevelProgress.Unlock(level.nextLevel.levelId);
            else
                LevelProgress.MarkCampaignComplete();

            // UIManager escucha OnLevelComplete y muestra el LevelEndPanel.
            EventBus.Dispatch_LevelComplete();
        }

        // ============================================================
        //   Navegacion entre escenas
        // ============================================================

        /// <summary>Carga el siguiente nivel de la campania (si hay).</summary>
        public void LoadNextLevel()
        {
            if (level != null && level.nextLevel != null)
                LoadLevel(level.nextLevel);
        }

        /// <summary>Recarga la escena actual (boton Reintentar).</summary>
        public void ReloadLevel()
        {
            if (level != null && !string.IsNullOrEmpty(level.sceneName))
                SceneManager.LoadScene(level.sceneName);
        }

        /// <summary>Helper estatico para cargar un nivel por su definicion (menu de seleccion).</summary>
        public static void LoadLevel(LevelDefinition def)
        {
            if (def != null && !string.IsNullOrEmpty(def.sceneName))
                SceneManager.LoadScene(def.sceneName);
        }
    }
}
