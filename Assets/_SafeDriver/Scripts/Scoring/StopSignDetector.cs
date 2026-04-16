using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Detecta si el vehiculo se detuvo completamente en una senal PARE.
    /// Se usa como componente en un trigger collider ubicado en la linea de detencion.
    /// </summary>
    public class StopSignDetector : InfractionDetector
    {
        [Header("Config")]
        [Tooltip("Velocidad en km/h por debajo de la cual se considera 'detenido'.")]
        [SerializeField] private float stopThresholdKmh = 2f;

        [Tooltip("Segundos que el auto debe estar detenido para contar como stop correcto.")]
        [SerializeField] private float requiredStopSeconds = 1.5f;

        private bool vehicleInZone;
        private float stoppedTimer;
        private bool alreadyEvaluated;

        void Reset()
        {
            infractionType = InfractionType.FailedToStopAtSign;
            pedagogicalMessage = "Art. 44 Ley 24.449: La senal PARE obliga a detener el vehiculo por completo "
                               + "y verificar que la via esta despejada antes de avanzar.";
        }

        void OnEnable()
        {
            if (infractionType == default) Reset();
        }

        void OnTriggerEnter(Collider other)
        {
            if (alreadyEvaluated) return;
            var vc = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<SafeDriver.Vehicle.VehicleController>() : null;
            if (vc == null) return;
            vehicleInZone = true;
            stoppedTimer = 0f;
        }

        void OnTriggerStay(Collider other)
        {
            if (alreadyEvaluated || !vehicleInZone) return;
            var vc = SafeDriver.Vehicle.VehicleController.Instance;
            if (vc == null) return;

            // Medir velocidad usando EventBus seria indirecto; accedemos via Instance (Vehicle es dependencia)
            float speed = other.attachedRigidbody.linearVelocity.magnitude * 3.6f;
            if (speed <= stopThresholdKmh)
            {
                stoppedTimer += Time.deltaTime;
                if (stoppedTimer >= requiredStopSeconds)
                {
                    alreadyEvaluated = true;
                    TriggerCorrectAction(ActionType.StoppedAtPareSign);
                }
            }
            else
            {
                stoppedTimer = 0f;
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (alreadyEvaluated) return;
            var vc = other.attachedRigidbody ? other.attachedRigidbody.GetComponent<SafeDriver.Vehicle.VehicleController>() : null;
            if (vc == null) return;

            // Salio de la zona sin haberse detenido el tiempo suficiente
            if (stoppedTimer < requiredStopSeconds)
            {
                alreadyEvaluated = true;
                TriggerInfraction();
            }
            vehicleInZone = false;
        }

        public void ResetDetector()
        {
            vehicleInZone = false;
            stoppedTimer = 0f;
            alreadyEvaluated = false;
        }
    }
}
