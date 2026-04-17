using UnityEngine;
using UnityEngine.InputSystem;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Lee input del jugador via Unity Input System (XR bindings).
    /// - Throttle action  = gatillo derecho
    /// - Brake action     = gatillo izquierdo
    /// - Steering         = seteado externamente por SteeringWheelController
    ///
    /// Las InputActions se asignan por Inspector. Bindings esperados:
    ///   throttleAction → &lt;XRController&gt;{RightHand}/trigger  (Value / Axis)
    ///   brakeAction    → &lt;XRController&gt;{LeftHand}/trigger   (Value / Axis)
    /// </summary>
    public class VehicleInput : MonoBehaviour
    {
        public static VehicleInput Instance { get; private set; }

        [Header("Input Actions (XR triggers)")]
        [SerializeField] private InputActionProperty throttleAction;
        [SerializeField] private InputActionProperty brakeAction;

        public float SteerInput    { get; private set; }
        public float ThrottleInput { get; private set; }
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

        void OnEnable()
        {
            throttleAction.action?.Enable();
            brakeAction.action?.Enable();
        }

        void OnDisable()
        {
            throttleAction.action?.Disable();
            brakeAction.action?.Disable();
        }

        void Update()
        {
            ThrottleInput = throttleAction.action?.ReadValue<float>() ?? 0f;
            BrakeInput    = brakeAction.action?.ReadValue<float>()    ?? 0f;
        }

        public void SetSteering(float value) { SteerInput = Mathf.Clamp(value, -1f, 1f); }

        public void SetThrottle(float value) { ThrottleInput = Mathf.Clamp01(value); }
        public void SetBrake(float value)    { BrakeInput    = Mathf.Clamp01(value); }
    }
}
