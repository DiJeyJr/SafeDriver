using System.Collections;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Controla las fisicas del vehiculo.
    /// Requiere Rigidbody + 4x WheelCollider + 4x Transform de mesh visual.
    /// Lee input de VehicleInput.Instance y aplica motor/steer/brake a las wheel colliders.
    /// Publica SpeedChanged via EventBus cada FixedUpdate.
    /// En SafeFail (escuchado via OnGameStateChanged) ejecuta SmoothStop (decel suave sin slam).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehicleController : MonoBehaviour
    {
        public static VehicleController Instance { get; private set; }

        [Header("Config")]
        public float maxSpeed     = 80f;   // km/h, limite urbano
        public float acceleration = 12f;   // escala de motor torque
        public float brakeForce   = 30f;   // escala de brake torque
        public float steerAngle   = 32f;   // grados, front wheels

        [Header("Wheel Colliders")]
        public WheelCollider wheelFL;
        public WheelCollider wheelFR;
        public WheelCollider wheelRL;
        public WheelCollider wheelRR;

        [Header("Wheel Meshes (visuales)")]
        public Transform wheelMeshFL;
        public Transform wheelMeshFR;
        public Transform wheelMeshRL;
        public Transform wheelMeshRR;

        [Header("SafeFail")]
        [Tooltip("Segundos para detener el vehiculo en SafeFail.")]
        [SerializeField] private float smoothStopSeconds = 1.5f;

        private Rigidbody rb;
        private float currentSpeed; // km/h
        private bool isSmoothStopping;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            rb = GetComponent<Rigidbody>();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            EventBus.OnGameStateChanged += HandleGameStateChanged;
        }

        void OnDisable()
        {
            EventBus.OnGameStateChanged -= HandleGameStateChanged;
        }

        void FixedUpdate()
        {
            if (isSmoothStopping)
            {
                UpdateWheelMeshes();
                UpdateSpeedAndDispatch();
                return;
            }

            var input = VehicleInput.Instance;
            if (input != null)
            {
                ApplyMotor(input.ThrottleInput);
                ApplySteering(input.SteerInput);
                ApplyBrakes(input.BrakeInput);
            }

            UpdateWheelMeshes();
            UpdateSpeedAndDispatch();
        }

        // ============================================================
        //   API publica
        // ============================================================

        /// <summary>Detiene el vehiculo suavemente en ~smoothStopSeconds. Comfort-VR friendly.</summary>
        public void SmoothStop()
        {
            if (isSmoothStopping) return;
            isSmoothStopping = true;
            StartCoroutine(SmoothStopRoutine());
        }

        // ============================================================
        //   Internals
        // ============================================================

        private IEnumerator SmoothStopRoutine()
        {
            // Corta motor inmediatamente
            wheelRL.motorTorque = 0f;
            wheelRR.motorTorque = 0f;

            // Rampea el freno de 0 -> 1 en smoothStopSeconds
            float t = 0f;
            while (currentSpeed > 0.1f && t < smoothStopSeconds * 1.5f)
            {
                float brakeRamp = Mathf.Clamp01(t / smoothStopSeconds);
                ApplyBrakes(brakeRamp);
                t += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }

            // Detener por completo
            ApplyBrakes(1f);
            if (rb != null) rb.linearVelocity = Vector3.zero;
            yield return new WaitForFixedUpdate();
            ApplyBrakes(0f);

            isSmoothStopping = false;
        }

        private void HandleGameStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.SafeFail) SmoothStop();
        }

        // ============================================================
        //   Helpers WheelCollider
        // ============================================================

        private void ApplyMotor(float input)
        {
            // RWD: tracción trasera. Si supera maxSpeed, corta el motor pero mantiene momentum.
            float torque = input * acceleration * 100f;
            if (currentSpeed >= maxSpeed) torque = 0f;
            if (wheelRL != null) wheelRL.motorTorque = torque;
            if (wheelRR != null) wheelRR.motorTorque = torque;
        }

        private void ApplySteering(float input)
        {
            float angle = input * steerAngle;
            if (wheelFL != null) wheelFL.steerAngle = angle;
            if (wheelFR != null) wheelFR.steerAngle = angle;
        }

        private void ApplyBrakes(float input)
        {
            float torque = input * brakeForce * 100f;
            if (wheelFL != null) wheelFL.brakeTorque = torque;
            if (wheelFR != null) wheelFR.brakeTorque = torque;
            if (wheelRL != null) wheelRL.brakeTorque = torque;
            if (wheelRR != null) wheelRR.brakeTorque = torque;
        }

        private void UpdateWheelMeshes()
        {
            SyncWheel(wheelFL, wheelMeshFL);
            SyncWheel(wheelFR, wheelMeshFR);
            SyncWheel(wheelRL, wheelMeshRL);
            SyncWheel(wheelRR, wheelMeshRR);
        }

        private static void SyncWheel(WheelCollider col, Transform mesh)
        {
            if (col == null || mesh == null) return;
            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.SetPositionAndRotation(pos, rot);
        }

        private void UpdateSpeedAndDispatch()
        {
            // linearVelocity (Unity 6) == velocity en versiones anteriores
            Vector3 v = rb != null ? rb.linearVelocity : Vector3.zero;
            currentSpeed = v.magnitude * 3.6f; // m/s -> km/h
            EventBus.Dispatch_SpeedChanged(currentSpeed);
        }
    }
}
