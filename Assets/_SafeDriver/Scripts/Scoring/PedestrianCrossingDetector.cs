using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Zona de control invisible en senda peatonal.
    /// Agregar como componente a un GameObject con BoxCollider (isTrigger=true)
    /// cubriendo la senda peatonal completa.
    ///
    /// NPCPedestrianAI llama SetPedestriansPresent(true) cuando un peaton pisa la senda,
    /// y SetPedestriansPresent(false) cuando termina de cruzar.
    ///
    /// Flujo:
    ///   1. Peaton pisa senda → NPCPedestrianAI → SetPedestriansPresent(true)
    ///   2. Auto entra en la zona → OnTriggerEnter:
    ///      - Si hay peatones Y auto no frenó → TriggerInfraction()
    ///   3. Auto permanece en zona → OnTriggerStay:
    ///      - Si hay peatones Y auto detenido por >requiredStopDuration → premio
    ///   4. Auto sale → OnTriggerExit: reset flags
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PedestrianCrossingDetector : InfractionDetector, IPedestrianCrossingNotifier
    {
        [Header("Config")]
        [Tooltip("Segundos que el auto debe estar detenido para contar como 'cedió el paso'.")]
        public float requiredStopDuration = 0.8f;

        private bool pedestriansPresent;
        private float stoppedTime;
        private bool infractionFired;
        private bool rewardGiven;

        void Awake()
        {
            infractionType = InfractionType.PedestrianNotYielded;
            pedagogicalMessage =
                "Ley 24.449 Art. 41 — El peaton tiene prioridad absoluta en la "
                + "senda peatonal. Debes detenerte y esperar que crucen.";
        }

        /// <summary>
        /// Llamado por NPCPedestrianAI cuando un peaton entra o sale de la senda.
        /// </summary>
        public void SetPedestriansPresent(bool present)
        {
            pedestriansPresent = present;
            if (present)
            {
                // Resetear contadores al aparecer nuevos peatones
                stoppedTime = 0f;
                infractionFired = false;
                rewardGiven = false;
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;

            if (pedestriansPresent
                && !infractionFired
                && VehicleController.Instance != null
                && !VehicleController.Instance.IsStopped())
            {
                // El auto entro a la senda con peatones SIN estar frenado.
                infractionFired = true;
                TriggerInfraction();
            }
        }

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            if (rewardGiven || infractionFired) return;

            if (pedestriansPresent
                && VehicleController.Instance != null
                && VehicleController.Instance.IsStopped())
            {
                stoppedTime += Time.deltaTime;
                if (stoppedTime >= requiredStopDuration)
                {
                    rewardGiven = true;
                    TriggerCorrectAction(ActionType.YieldedToPedestrian);
                }
            }
            else
            {
                // Si el auto empezo a moverse, resetear el timer
                stoppedTime = 0f;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("PlayerVehicle")) return;
            stoppedTime = 0f;
        }
    }
}
