using UnityEngine;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Lee input del jugador desde los controllers Quest via OVRInput.
    /// - Gatillo derecho (Secondary trigger) = acelerador
    /// - Gatillo izquierdo (Primary trigger) = freno
    /// - Steering: seteado externamente por SteeringWheelController (NO se lee de manos)
    ///
    /// Nota: hand tracking esta desactivado. Todo el input es via controllers.
    /// </summary>
    public class VehicleInput : MonoBehaviour
    {
        public static VehicleInput Instance { get; private set; }

        /// <summary>Steering normalizado. -1 = izquierda, +1 = derecha. Seteado por SteeringWheelController.</summary>
        public float SteerInput    { get; private set; }

        /// <summary>Acelerador normalizado. 0 = sin acelerar, 1 = full.</summary>
        public float ThrottleInput { get; private set; }

        /// <summary>Freno normalizado. 0 = sin frenar, 1 = full.</summary>
        public float BrakeInput    { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            // Gatillo derecho = acelerador
            ThrottleInput = OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger);

            // Gatillo izquierdo = freno
            BrakeInput = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger);

            // SteerInput NO se lee aca — lo setea SteeringWheelController via SetSteering()
        }

        /// <summary>Llamado por SteeringWheelController cada frame para setear el steering.</summary>
        public void SetSteering(float value) { SteerInput = Mathf.Clamp(value, -1f, 1f); }

        /// <summary>Override para debug en editor sin headset.</summary>
        public void SetThrottle(float value) { ThrottleInput = Mathf.Clamp01(value); }
        public void SetBrake(float value)    { BrakeInput    = Mathf.Clamp01(value); }
    }
}
