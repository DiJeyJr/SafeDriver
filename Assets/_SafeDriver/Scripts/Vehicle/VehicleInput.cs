using UnityEngine;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Lee input del jugador desde los controllers Quest via OVRInput:
    /// - Gatillo derecho (Secondary trigger) = acelerador
    /// - Gatillo izquierdo (Primary trigger) = freno
    /// - Rotacion del volante = diferencia de altura entre manos (L/R)
    ///
    /// Expone los valores normalizados a VehicleController via:
    ///   VehicleInput.Instance.ThrottleInput / BrakeInput / SteerInput
    /// </summary>
    public class VehicleInput : MonoBehaviour
    {
        public static VehicleInput Instance { get; private set; }

        [Header("Config volante")]
        [Tooltip("Angulo maximo del volante en grados (lock-to-lock / 2). Tipico auto urbano: 450-540.")]
        [SerializeField] private float maxSteeringAngle = 450f;

        [Tooltip("Zona muerta en grados para evitar temblores cuando las manos estan casi a la par.")]
        [SerializeField] private float deadZoneAngle = 3f;

        [Tooltip("Suavizado temporal del steering (0 = sin suavizar, 1 = muy suave).")]
        [Range(0f, 0.95f)]
        [SerializeField] private float steerSmoothing = 0.25f;

        // ============================================================
        //   Outputs (lectura desde VehicleController)
        // ============================================================
        public float ThrottleInput { get; private set; }   // 0..1
        public float BrakeInput    { get; private set; }   // 0..1
        public float SteerInput    { get; private set; }   // -1..+1

        /// <summary>Angulo calculado del volante virtual en grados. Util para rotar el mesh del wheel.</summary>
        public float SteeringWheelAngleDeg { get; private set; }

        private float steerInputFiltered;

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

            // Steering desde la posicion relativa de las manos
            SteeringWheelAngleDeg = CalculateSteerFromHands();

            float raw = Mathf.Clamp(SteeringWheelAngleDeg / maxSteeringAngle, -1f, 1f);

            // Suavizado temporal para evitar jitter en manos tracking
            steerInputFiltered = Mathf.Lerp(raw, steerInputFiltered, steerSmoothing);
            SteerInput = steerInputFiltered;
        }

        /// <summary>
        /// Calcula el angulo del volante (en grados) en base a la linea entre ambas manos.
        /// Convencion: volante horizontal (manos niveladas) = 0 grados.
        /// Girar a la derecha (mano derecha baja, mano izquierda sube) = +grados.
        /// Girar a la izquierda (mano izquierda baja, mano derecha sube) = -grados.
        /// </summary>
        public float CalculateSteerFromHands()
        {
            Vector3 leftHand  = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            Vector3 rightHand = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);

            // Vector entre manos (L -> R). Con manos a la par es (largo, 0, algo).
            Vector3 handVector = rightHand - leftHand;

            // atan2(x, y) con la convencion local:
            //   manos niveladas  -> (2r, 0)   -> atan2 = 90   -> centrado = 0
            //   giro a la derecha -> (0, -2r) -> atan2 = 180  -> centrado = +90
            //   giro a la izq.    -> (0, +2r) -> atan2 = 0    -> centrado = -90
            float rawAngle = Mathf.Atan2(handVector.x, handVector.y) * Mathf.Rad2Deg;
            float centered = rawAngle - 90f;

            // Zona muerta para temblores
            if (Mathf.Abs(centered) < deadZoneAngle) centered = 0f;

            return centered;
        }

        // ============================================================
        //   Setters opcionales (para debug/teclado en editor sin headset)
        //   Se invocan via reflexion o script debug si hace falta.
        // ============================================================
        public void OverrideThrottle(float value) { ThrottleInput = Mathf.Clamp01(value); }
        public void OverrideBrake(float value)    { BrakeInput    = Mathf.Clamp01(value); }
        public void OverrideSteer(float value)    { SteerInput    = Mathf.Clamp(value, -1f, 1f); steerInputFiltered = SteerInput; }
    }
}
