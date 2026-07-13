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
        private bool levelEnded; // evita doble cierre (misiones completas + llegada a la meta)

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
            EventBus.OnScoreChanged += HandleScoreChanged;
            EventBus.OnCorrectActionPerformed += HandleCorrectAction;
        }

        void OnDisable()
        {
            EventBus.OnScoreChanged -= HandleScoreChanged;
            EventBus.OnCorrectActionPerformed -= HandleCorrectAction;
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

        // Llegar a la meta (ultimo checkpoint) termina el nivel SI O SI, aunque queden
        // misiones sin hacer: el resumen final muestra que se hizo y que no. El desbloqueo
        // del siguiente nivel sigue exigiendo completar las misiones obligatorias.
        private void HandleCorrectAction(SafeDriver.Core.ActionType type, int bonus)
        {
            if (type == SafeDriver.Core.ActionType.ReachedGoal && !levelEnded && level != null)
                StartCoroutine(EndByGoalDeferred());
        }

        // Un frame de espera: deja que la mision "Llegar a la meta" procese este mismo
        // evento y, si con eso quedan todas completas, gane el cierre CON desbloqueo.
        private System.Collections.IEnumerator EndByGoalDeferred()
        {
            yield return null;
            EndLevel(unlock: false);
        }

        private void HandleAllRequiredCompleted() => EndLevel(unlock: true);

        private void EndLevel(bool unlock)
        {
            if (levelEnded) return;
            levelEnded = true;

            LevelProgress.ReportScore(level.levelId, lastScore);

            if (unlock)
            {
                if (level.nextLevel != null)
                    LevelProgress.Unlock(level.nextLevel.levelId);
                else
                    LevelProgress.MarkCampaignComplete();
            }

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
