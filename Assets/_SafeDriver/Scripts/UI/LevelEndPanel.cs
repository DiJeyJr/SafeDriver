using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SafeDriver.Core;
using SafeDriver.Missions;
using SafeDriver.Scoring;

namespace SafeDriver.UI
{
    /// <summary>
    /// Panel de fin de nivel: muestra resumen (score, infracciones, tiempo, aprobado/reprobado).
    /// Lee el LevelResult de ScoreManager.Instance.GetLevelResult().
    /// Botones: Reintentar (recarga la escena) / Menu principal (carga MainMenu).
    /// </summary>
    public class LevelEndPanel : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI infractionsListText;

        [Header("Botones")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;
        [Tooltip("Boton 'Siguiente' — solo se muestra si hay proximo nivel y esta desbloqueado.")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryPressed);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuPressed);
            if (nextLevelButton != null) nextLevelButton.onClick.AddListener(OnNextLevelPressed);
        }

        void OnEnable()
        {
            // Cuando UIManager activa este GameObject (al entrar a LevelEnd), refresca el resumen.
            Show();
        }

        // ============================================================
        //   Botones
        // ============================================================

        private void OnRetryPressed()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnMainMenuPressed()
        {
            Time.timeScale = 1f;
            if (!string.IsNullOrEmpty(mainMenuSceneName))
                SceneManager.LoadScene(mainMenuSceneName);
        }

        private void OnNextLevelPressed()
        {
            Time.timeScale = 1f;
            if (LevelManager.Instance != null) LevelManager.Instance.LoadNextLevel();
        }

        public void Show()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            if (titleText != null) titleText.text = "FIN DEL NIVEL";

            // "Siguiente" solo si hay proximo nivel y quedo desbloqueado (misiones completas).
            if (nextLevelButton != null)
            {
                var lm = LevelManager.Instance;
                bool hayNext = lm != null && lm.Level != null && lm.Level.nextLevel != null;
                bool desbloqueado = hayNext && LevelProgress.IsUnlocked(lm.Level.nextLevel.levelId);
                nextLevelButton.gameObject.SetActive(hayNext && desbloqueado);
            }

            if (ScoreManager.Instance == null) return;
            LevelResult result = ScoreManager.Instance.GetLevelResult();

            if (scoreText != null)
                scoreText.text = result.FinalScore.ToString();

            if (statusText != null)
                statusText.text = result.Passed ? "APROBADO" : "REPROBADO";

            if (infractionsListText != null)
            {
                var sb = new System.Text.StringBuilder();

                // Que hiciste y que no: checklist de objetivos del nivel.
                if (MissionManager.Instance != null && MissionManager.Instance.ActiveMissions.Count > 0)
                {
                    sb.AppendLine("<b>Objetivos:</b>");
                    foreach (var m in MissionManager.Instance.ActiveMissions)
                    {
                        bool ok = m.Status == MissionStatus.Completed;
                        sb.AppendLine((ok ? "<color=#4AC262>✔</color> " : "<color=#F16161>✘</color> ")
                                      + m.Definition.title);
                    }
                    sb.AppendLine();
                }

                if (result.Infractions.Count > 0)
                    sb.AppendLine("<b>Infracciones:</b>");
                foreach (var inf in result.Infractions)
                {
                    int min = Mathf.FloorToInt(inf.TimeSeconds / 60f);
                    int sec = Mathf.FloorToInt(inf.TimeSeconds % 60f);
                    sb.AppendLine(string.Format("[{0:00}:{1:00}] {2}", min, sec, inf.Message));
                }
                infractionsListText.text = sb.ToString();
            }
        }

        public void Hide()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
        }
    }
}
