using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Scoring;

namespace SafeDriver.UI
{
    /// <summary>
    /// Panel de fin de nivel: muestra resumen (score, infracciones, tiempo) + opciones "Reintentar" / "Siguiente".
    /// </summary>
    public class LevelEndPanel : MonoBehaviour
    {
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private UnityEngine.UI.Text summaryText;

        public void Show(int finalScore, int infractionCount, float elapsedSeconds) { /* TODO */ }
    }
}
