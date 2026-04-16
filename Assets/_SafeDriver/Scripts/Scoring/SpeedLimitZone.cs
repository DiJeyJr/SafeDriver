using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Zona con limite de velocidad especifico.
    /// Colocar como Trigger en cada tramo de calle con limite diferente.
    /// Cuando el vehiculo entra, notifica al HUD del nuevo limite via EventBus.
    /// Mientras el vehiculo esta en la zona:
    ///   - Si supera speedLimitKmH + warningThreshold → TriggerInfraction(Speeding)
    ///   - Si mantiene velocidad legal por POINTS_INTERVAL seg → TriggerCorrectAction(+2)
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SpeedLimitZone : InfractionDetector
    {
        [Header("Configuracion de zona")]
        [Tooltip("Limite de velocidad de esta zona en km/h.")]
        public float speedLimitKmH = 40f;

        [Tooltip("Margen en km/h antes de que se dispare la infraccion (ej: 5 = tolera hasta 45 en zona de 40).")]
        public float warningThreshold = 5f;

        [Header("Premio por mantener velocidad legal")]
        [Tooltip("Segundos de conduccion legal entre cada premio.")]
        [SerializeField] private float pointsInterval = 5f;

        private bool isVehicleInZone;
        private float legalTimeAccumulator;
        private bool currentlySpeeding;

        void Awake()
        {
            infractionType = InfractionType.Speeding;
            pedagogicalMessage =
                "Ley 24.449 Art. 51 — Velocidad maxima en esta zona: "
                + speedLimitKmH.ToString("0") + " km/h. "
                + "Exceder el limite pone en riesgo a peatones y otros conductores.";
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            isVehicleInZone = true;
            legalTimeAccumulator = 0f;
            currentlySpeeding = false;

            // Notificar al HUD del nuevo limite de velocidad de esta zona
            EventBus.Dispatch_SpeedLimitChanged(speedLimitKmH);
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            isVehicleInZone = false;
            currentlySpeeding = false;
            legalTimeAccumulator = 0f;
        }

        void Update()
        {
            if (!isVehicleInZone) return;
            if (VehicleController.Instance == null) return;

            float currentSpeed = VehicleController.Instance.CurrentSpeedKmh;

            if (currentSpeed > speedLimitKmH + warningThreshold)
            {
                // Excediendo — disparar infraccion solo en el rising edge (no spam cada frame)
                if (!currentlySpeeding)
                {
                    currentlySpeeding = true;
                    legalTimeAccumulator = 0f;

                    // Actualizar el mensaje con la velocidad actual
                    pedagogicalMessage =
                        "Ley 24.449 Art. 51 — Velocidad maxima en esta zona: "
                        + speedLimitKmH.ToString("0") + " km/h. "
                        + "Velocidad detectada: " + currentSpeed.ToString("0") + " km/h.";

                    TriggerInfraction();
                }
            }
            else
            {
                // Dentro del limite
                currentlySpeeding = false;

                // Solo acumular si el auto se mueve (evitar premiar al que esta estacionado)
                if (currentSpeed > 2f)
                {
                    legalTimeAccumulator += Time.deltaTime;
                    if (legalTimeAccumulator >= pointsInterval)
                    {
                        legalTimeAccumulator = 0f;
                        TriggerCorrectAction(ActionType.MaintainedLegalSpeed);
                    }
                }
            }
        }
    }
}
