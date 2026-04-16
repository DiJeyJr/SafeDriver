using UnityEngine;
using UnityEngine.UI;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// HUD in-game: velocimetro, score, limite de velocidad, notificaciones de infraccion.
    /// Todo suscripto a EventBus — sin dependencias directas a Vehicle/Scoring.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Textos")]
        [SerializeField] private Text speedText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text speedLimitText;
        [SerializeField] private Text notificationText;

        [Header("Config")]
        [Tooltip("Segundos que se muestra la notificacion de infraccion antes de ocultarse.")]
        [SerializeField] private float notificationDuration = 4f;

        void OnEnable()
        {
            EventBus.OnSpeedChanged       += HandleSpeedChanged;
            EventBus.OnScoreChanged       += HandleScoreChanged;
            EventBus.OnSpeedLimitChanged  += HandleSpeedLimitChanged;
            EventBus.OnInfractionDetected += HandleInfraction;
        }

        void OnDisable()
        {
            EventBus.OnSpeedChanged       -= HandleSpeedChanged;
            EventBus.OnScoreChanged       -= HandleScoreChanged;
            EventBus.OnSpeedLimitChanged  -= HandleSpeedLimitChanged;
            EventBus.OnInfractionDetected -= HandleInfraction;
        }

        private void HandleSpeedChanged(float speedKmh)
        {
            if (speedText != null) speedText.text = speedKmh.ToString("0") + " km/h";
        }

        private void HandleScoreChanged(int newTotal)
        {
            if (scoreText != null) scoreText.text = newTotal.ToString();
        }

        private void HandleSpeedLimitChanged(float limitKmh)
        {
            if (speedLimitText != null) speedLimitText.text = limitKmh.ToString("0");
        }

        private void HandleInfraction(InfractionType type, string message)
        {
            if (notificationText != null)
            {
                notificationText.text = message;
                CancelInvoke(nameof(ClearNotification));
                Invoke(nameof(ClearNotification), notificationDuration);
            }
        }

        private void ClearNotification()
        {
            if (notificationText != null) notificationText.text = "";
        }
    }
}
