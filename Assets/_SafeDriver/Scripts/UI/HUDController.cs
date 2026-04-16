using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Gestiona todos los elementos de UI en VR.
    ///
    /// UI DIEGETICA (en el tablero del auto):
    ///   - Velocimetro con aguja rotante (SpeedometerNeedle)
    ///   - Display de score (TextMeshPro en tablero)
    ///   - Senal de limite de velocidad (TextMeshPro)
    ///
    /// UI NO-DIEGETICA (world space, near driver):
    ///   - Panel de notificacion temporal (infraccion/acierto)
    ///   - Aparece 3 seg y se desvanece, sin bloquear conduccion
    ///
    /// REGLA VR: la UI diegetica esta SIEMPRE visible en el tablero.
    /// Las notificaciones flotan brevemente y desaparecen.
    /// NUNCA bloquear la vista del conductor.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("UI Diegetica (en tablero del auto)")]
        [Tooltip("Componente SpeedometerNeedle en la aguja del velocimetro del tablero.")]
        public SpeedometerNeedle speedometerNeedle;

        [Tooltip("TextMeshPro del display de score en el tablero.")]
        public TextMeshPro scoreDisplay;

        [Tooltip("TextMeshPro de la senal de limite de velocidad.")]
        public TextMeshPro speedLimitSign;

        [Header("UI No-Diegetica (world space, cerca del conductor)")]
        [Tooltip("Panel popup que aparece brevemente ante infraccion/acierto.")]
        public GameObject notificationPanel;

        [Tooltip("Texto de la notificacion.")]
        public TextMeshPro notificationText;

        [Tooltip("Icono de la notificacion (Image UI o SpriteRenderer segun tu setup).")]
        public Image notificationIcon;

        [Header("Sprites de notificacion")]
        public Sprite infractionSprite;
        public Sprite successSprite;

        [Header("Config")]
        [Tooltip("Segundos que se muestra la notificacion de infraccion.")]
        [SerializeField] private float infractionDuration = 3f;

        [Tooltip("Segundos que se muestra la notificacion de acierto.")]
        [SerializeField] private float successDuration = 2f;

        private Coroutine activeNotification;

        void OnEnable()
        {
            EventBus.OnSpeedChanged            += UpdateSpeedometer;
            EventBus.OnScoreChanged            += UpdateScoreDisplay;
            EventBus.OnSpeedLimitChanged       += UpdateSpeedLimit;
            EventBus.OnInfractionDetected      += ShowInfractionNotification;
            EventBus.OnCorrectActionPerformed  += ShowSuccessNotification;
        }

        void OnDisable()
        {
            EventBus.OnSpeedChanged            -= UpdateSpeedometer;
            EventBus.OnScoreChanged            -= UpdateScoreDisplay;
            EventBus.OnSpeedLimitChanged       -= UpdateSpeedLimit;
            EventBus.OnInfractionDetected      -= ShowInfractionNotification;
            EventBus.OnCorrectActionPerformed  -= ShowSuccessNotification;
        }

        // ============================================================
        //   Diegetica: tablero del auto
        // ============================================================

        private void UpdateSpeedometer(float speedKmh)
        {
            if (speedometerNeedle != null)
                speedometerNeedle.SetSpeed(speedKmh);
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreDisplay != null)
                scoreDisplay.text = score.ToString();
        }

        private void UpdateSpeedLimit(float limitKmh)
        {
            if (speedLimitSign != null)
                speedLimitSign.text = limitKmh.ToString("0");
        }

        // ============================================================
        //   No-diegetica: notificaciones temporales
        // ============================================================

        private void ShowInfractionNotification(InfractionType type, string message)
        {
            ShowNotification(message, infractionSprite, new Color(0.9f, 0.2f, 0.2f), infractionDuration);
        }

        private void ShowSuccessNotification(ActionType type, int bonus)
        {
            string msg = "+" + bonus + " " + type.ToString();
            ShowNotification(msg, successSprite, new Color(0.2f, 0.8f, 0.3f), successDuration);
        }

        private void ShowNotification(string message, Sprite icon, Color tint, float duration)
        {
            if (activeNotification != null)
                StopCoroutine(activeNotification);
            activeNotification = StartCoroutine(NotificationRoutine(message, icon, tint, duration));
        }

        private IEnumerator NotificationRoutine(string message, Sprite icon, Color tint, float duration)
        {
            // Mostrar
            if (notificationPanel != null) notificationPanel.SetActive(true);
            if (notificationText != null)
            {
                notificationText.text = message;
                notificationText.color = tint;
            }
            if (notificationIcon != null && icon != null)
            {
                notificationIcon.sprite = icon;
                notificationIcon.color = tint;
            }

            yield return new WaitForSeconds(duration);

            // Ocultar
            if (notificationPanel != null) notificationPanel.SetActive(false);
            activeNotification = null;
        }
    }
}
