using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using SafeDriver.Core;
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
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        void Awake()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryPressed);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuPressed);
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

        public void Show()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            if (titleText != null) titleText.text = "FIN DEL NIVEL";

            if (ScoreManager.Instance == null) return;
            LevelResult result = ScoreManager.Instance.GetLevelResult();

            if (scoreText != null)
                scoreText.text = result.FinalScore.ToString();

            if (statusText != null)
                statusText.text = result.Passed ? "APROBADO" : "REPROBADO";

            if (infractionsListText != null)
            {
                var sb = new System.Text.StringBuilder();
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
