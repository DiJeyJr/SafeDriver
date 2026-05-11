using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Palanca de cambios D/N/R. Detecta su rotacion local en X y mapea al GearState
    /// correspondiente, notificando a VehicleController.SetGearFromShifter.
    ///
    /// Layout esperado: GameObject hijo de un pivot cuya rotacion X local va de -30 a +30.
    /// El usuario lo agarra con la mano VR y empuja:
    ///   - Adelante (X > +deadzone) → Drive
    ///   - Centro  (|X| < deadzone) → Neutral
    ///   - Atras   (X < -deadzone) → Reverse
    ///
    /// Tambien notifica al `vehicle` cuando se hace dirty. Si el VehicleController tiene
    /// `useExternalShifter=true`, esta palanca es la unica autoridad sobre el gear.
    /// </summary>
    public class GearShifter : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("VehicleController al que se le setean los gears. Si queda vacio, se busca el de la escena en Start.")]
        [SerializeField] private VehicleController vehicle;

        [Tooltip("Transform cuya rotacion X local define el gear. Tipicamente el pivot de la palanca.")]
        [SerializeField] private Transform pivot;

        [Header("Config")]
        [Tooltip("Angulo (grados) dentro del cual la palanca esta en Neutral. Fuera, va a D o R.")]
        [SerializeField] private float neutralDeadzone = 12f;

        [Tooltip("Si esta activo, fuerza al pivot a snapearse a -angle/0/+angle cuando se suelta. Requiere armar Grabbable externo.")]
        [SerializeField] private bool snapOnRelease = false;

        [Tooltip("Angulo objetivo (grados) para D y R cuando snapOnRelease=true.")]
        [SerializeField] private float snapAngle = 22f;

        private GearState lastGear = GearState.Neutral;
        private Vector3 originalLocalPosition;

        void Awake()
        {
            originalLocalPosition = transform.localPosition;
        }

        void Start()
        {
            if (vehicle == null) vehicle = FindFirstObjectByType<VehicleController>();
            if (pivot == null) pivot = transform;

            // Publicar el estado inicial segun la rotacion actual
            EvaluateAndDispatch(force: true);
        }

        void Update()
        {
            EvaluateAndDispatch(force: false);
        }

        // Lock posicional: el OneGrabRotateTransformer solo modifica rotacion, pero durante
        // el grab la posicion local puede drift si el rig se mueve. Forzar el local pos
        // original cada LateUpdate garantiza que la palanca siempre vuelva a su lugar.
        void LateUpdate()
        {
            if (transform.localPosition != originalLocalPosition)
                transform.localPosition = originalLocalPosition;
        }

        private void EvaluateAndDispatch(bool force)
        {
            float angle = NormalizeAngle(pivot.localEulerAngles.x);

            GearState gear;
            if (angle > neutralDeadzone)       gear = GearState.Drive;
            else if (angle < -neutralDeadzone) gear = GearState.Reverse;
            else                               gear = GearState.Neutral;

            if (!force && gear == lastGear) return;
            lastGear = gear;

            if (vehicle != null) vehicle.SetGearFromShifter(gear);
        }

        /// <summary>Devuelve angulo en rango (-180, 180].</summary>
        private static float NormalizeAngle(float a)
        {
            a %= 360f;
            if (a > 180f) a -= 360f;
            if (a <= -180f) a += 360f;
            return a;
        }
    }
}
