using UnityEngine;
using TMPro;
using SafeDriver.Core;
using SafeDriver.Scoring;

namespace SafeDriver.UI
{
    /// <summary>
    /// Panel de fin de nivel: muestra resumen (score, infracciones, tiempo, aprobado/reprobado).
    /// Lee el LevelResult de ScoreManager.Instance.GetLevelResult().
    /// </summary>
    public class LevelEndPanel : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private TextMeshPro scoreText;
        [SerializeField] private TextMeshPro statusText;
        [SerializeField] private TextMeshPro infractionsListText;

        public void Show()
        {
            if (rootPanel != null) rootPanel.SetActive(true);

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
