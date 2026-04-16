using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Observa el estado del vehiculo + estado del trafico y publica infracciones/aciertos
    /// via EventBus.Dispatch_Infraction / Dispatch_CorrectAction.
    /// </summary>
    public class InfractionDetector : MonoBehaviour
    {
        [SerializeField] private float speedLimitKmh = 40f;
        [SerializeField] private float speedingGraceKmh = 5f;

        private bool speeding;

        void OnEnable()
        {
            EventBus.OnSpeedChanged += HandleSpeedChanged;
        }

        void OnDisable()
        {
            EventBus.OnSpeedChanged -= HandleSpeedChanged;
        }

        private void HandleSpeedChanged(float speedKmh)
        {
            bool over = speedKmh > (speedLimitKmh + speedingGraceKmh);
            if (over && !speeding)
            {
                speeding = true;
                EventBus.Dispatch_Infraction(InfractionType.Speeding,
                    $"Speeding: {speedKmh:0} km/h (limit {speedLimitKmh:0})");
            }
            else if (!over && speeding)
            {
                speeding = false;
            }
        }
    }
}
