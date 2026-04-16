using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;
using SafeDriver.Traffic;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Zona de control invisible en semaforos.
    /// Agregar como componente a un GameObject con BoxCollider (isTrigger=true)
    /// ubicado en la linea de detencion del semaforo.
    ///
    /// Debe tener una referencia al TrafficLightController de la interseccion.
    /// El vehiculo del jugador debe tener el tag "PlayerVehicle".
    ///
    /// Flujo:
    ///   1. Auto entra en la zona (OnTriggerEnter)
    ///      → Si semaforo esta en rojo → TriggerInfraction() inmediato
    ///   2. Auto permanece en la zona (OnTriggerStay)
    ///      → Si semaforo esta en rojo Y auto esta detenido (IsStopped) por >0.5s
    ///        → TriggerCorrectAction(StoppedAtRedLight, +10) — el conductor freno bien
    ///   3. Auto sale de la zona (OnTriggerExit)
    ///      → Reset para la proxima pasada
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrafficLightDetector : InfractionDetector
    {
        [Header("Referencia")]
        [Tooltip("Arrastrar el TrafficLightController de este semaforo.")]
        [SerializeField] private TrafficLightController trafficLight;

        [Header("Config")]
        [Tooltip("Segundos detenido en rojo antes de dar premio (evita falsos positivos).")]
        [SerializeField] private float minStopTimeForReward = 0.5f;

        private bool vehicleIsInZone;
        private float entryTime;
        private bool infractionFired;
        private bool rewardGiven;

        void Awake()
        {
            infractionType = InfractionType.RanRedLight;
            pedagogicalMessage =
                "Ley 24.449 Art. 43 — El conductor debe detenerse completamente "
                + "ante la senal roja y esperar la luz verde.";
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            vehicleIsInZone = true;
            entryTime = Time.time;

            if (trafficLight != null && trafficLight.IsRed())
            {
                // El auto ENTRO a la zona con luz roja — infraccion inmediata.
                // (Si hubiera entrado en verde/amarillo y cambio a rojo mientras espera,
                //  eso no es infraccion — por eso el check es en Enter, no en Stay.)
                infractionFired = true;
                TriggerInfraction();
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            if (!vehicleIsInZone) return;

            // Solo premiar si: semaforo en rojo, auto detenido, no hubo infraccion previa,
            // y el auto lleva >minStopTimeForReward en la zona (evita reward por rozar el trigger).
            if (trafficLight != null
                && trafficLight.IsRed()
                && !infractionFired
                && !rewardGiven
                && VehicleController.Instance != null
                && VehicleController.Instance.IsStopped()
                && (Time.time - entryTime) > minStopTimeForReward)
            {
                rewardGiven = true;
                TriggerCorrectAction(ActionType.StoppedAtRedLight);
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            vehicleIsInZone = false;
            infractionFired = false;
            rewardGiven = false;
        }
    }
}
