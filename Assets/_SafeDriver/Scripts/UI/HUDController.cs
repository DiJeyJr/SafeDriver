using UnityEngine;
using UnityEngine.UI;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// HUD in-game: velocidad, score, notificaciones de infraccion.
    /// Suscribe a EventBus directamente — sin dependencias a Vehicle/Scoring.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private Text speedText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text notificationText;

        void OnEnable()
        {
            EventBus.OnSpeedChanged       += HandleSpeedChanged;
            EventBus.OnScoreChanged       += HandleScoreChanged;
            EventBus.OnInfractionDetected += HandleInfraction;
        }

        void OnDisable()
        {
            EventBus.OnSpeedChanged       -= HandleSpeedChanged;
            EventBus.OnScoreChanged       -= HandleScoreChanged;
            EventBus.OnInfractionDetected -= HandleInfraction;
        }

        private void HandleSpeedChanged(float speedKmh)
        {
            if (speedText != null) speedText.text = $"{speedKmh:0} km/h";
        }

        private void HandleScoreChanged(int newTotal)
        {
            if (scoreText != null) scoreText.text = newTotal.ToString();
        }

        private void HandleInfraction(InfractionType type, string message)
        {
            if (notificationText != null) notificationText.text = message;
        }
    }
}
