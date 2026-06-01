using UnityEngine;
using TMPro;
using SafeDriver.Core;

namespace SafeDriver.UI
{
    /// <summary>
    /// Gestiona la UI DIEGETICA del tablero del auto: velocimetro (aguja + numero),
    /// display de score y senal de limite de velocidad. Siempre visible, nunca bloquea
    /// la vista del conductor.
    ///
    /// El feedback de infracciones/aciertos NO se muestra aca: las infracciones graves
    /// usan la pantalla SafeFail (pedagogica) y los aciertos se reflejan en el panel de
    /// objetivos del tablero + haptica. El viejo popup de notificaciones se elimino.
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

        [Tooltip("TextMeshPro de velocidad ACTUAL del auto en km/h (numero grande en el tablero).")]
        public TextMeshPro currentSpeedDisplay;

        void OnEnable()
        {
            EventBus.OnSpeedChanged      += UpdateSpeedometer;
            EventBus.OnScoreChanged      += UpdateScoreDisplay;
            EventBus.OnSpeedLimitChanged += UpdateSpeedLimit;
        }

        void OnDisable()
        {
            EventBus.OnSpeedChanged      -= UpdateSpeedometer;
            EventBus.OnScoreChanged      -= UpdateScoreDisplay;
            EventBus.OnSpeedLimitChanged -= UpdateSpeedLimit;
        }

        private void UpdateSpeedometer(float speedKmh)
        {
            if (speedometerNeedle != null)
                speedometerNeedle.SetSpeed(speedKmh);
            if (currentSpeedDisplay != null)
                currentSpeedDisplay.text = Mathf.RoundToInt(speedKmh).ToString();
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
    }
}
