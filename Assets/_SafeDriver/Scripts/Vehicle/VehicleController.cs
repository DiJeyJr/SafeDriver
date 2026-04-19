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

        [Header("Reverse")]
        [Tooltip("Velocidad maxima en reversa, km/h.")]
        [SerializeField] private float maxReverseSpeedKmh = 10f;
        [Tooltip("Segundos sosteniendo el freno parado antes de cambiar de marcha (D<->R).")]
        [SerializeField] private float gearSwitchHoldSeconds = 2f;
        [Tooltip("Input de freno por encima del cual se considera 'sostenido' para armar cambio de marcha.")]
        [SerializeField, Range(0.1f, 1f)] private float gearSwitchBrakeThreshold = 0.5f;

        [Header("Debug")]
        [SerializeField] private bool logPhysics = false;
        private float lastLogTime;

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

        [Header("Stop Detection")]
        [Tooltip("Velocidad en km/h por debajo de la cual el vehiculo se considera 'detenido'.")]
        [SerializeField] private float stoppedThresholdKmh = 2f;

        private Rigidbody rb;
        private float currentSpeed; // km/h
        private bool isSmoothStopping;

        private GearState currentGear = GearState.Drive;
        private float gearSwitchTimer;
        // Latch: exige soltar el freno antes de permitir un arming. Se activa (a) al
        // completarse un arming previo y (b) al pasar de moviendo -> parado, para que el
        // freno que se venia sosteniendo al frenar no arme inmediatamente el cambio:
        // el usuario tiene que estar quieto y recien ahi re-apretar el gatillo.
        private bool brakeReleaseRequired;
        private bool wasStoppedLastFrame;

        public GearState CurrentGear => currentGear;

        // Offset de rotacion inicial de cada mesh respecto a la rotacion mundial de su WheelCollider.
        // Los meshes importados suelen traer una rotacion base (artistica) que debemos preservar; si
        // copiaramos tal cual la rotacion del collider, los meshes quedarian orientados al reves.
        private Quaternion meshOffsetFL = Quaternion.identity;
        private Quaternion meshOffsetFR = Quaternion.identity;
        private Quaternion meshOffsetRL = Quaternion.identity;
        private Quaternion meshOffsetRR = Quaternion.identity;

        /// <summary>True si la velocidad actual es menor al threshold de 'detenido'.</summary>
        public bool IsStopped() => currentSpeed < stoppedThresholdKmh;

        /// <summary>Velocidad actual del vehiculo en km/h.</summary>
        public float CurrentSpeedKmh => currentSpeed;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            meshOffsetFL = CaptureMeshOffset(wheelFL, wheelMeshFL);
            meshOffsetFR = CaptureMeshOffset(wheelFR, wheelMeshFR);
            meshOffsetRL = CaptureMeshOffset(wheelRL, wheelMeshRL);
            meshOffsetRR = CaptureMeshOffset(wheelRR, wheelMeshRR);

            EventBus.Dispatch_GearChanged(currentGear);
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
                UpdateGear(input.ThrottleInput, input.BrakeInput);
                ApplyMotor(input.ThrottleInput);
                ApplySteering(input.SteerInput);
                ApplyBrakes(input.BrakeInput);
            }
            else
            {
                UpdateGear(0f, 0f);
            }

            UpdateWheelMeshes();
            UpdateSpeedAndDispatch();

            if (logPhysics && Time.time - lastLogTime > 1f)
            {
                lastLogTime = Time.time;
                string inputState = input == null ? "INPUT==NULL" : $"t={input.ThrottleInput:F2} s={input.SteerInput:F2} b={input.BrakeInput:F2}";
                float rlTorque = wheelRL != null ? wheelRL.motorTorque : -1f;
                float rlRpm    = wheelRL != null ? wheelRL.rpm : -1f;
                bool rlGrounded = wheelRL != null && wheelRL.isGrounded;
                Vector3 vel = rb != null ? rb.linearVelocity : Vector3.zero;
                Debug.Log($"[VCDebug] {inputState} | RL torque={rlTorque:F0} rpm={rlRpm:F1} grounded={rlGrounded} | carSpeed={vel.magnitude*3.6f:F1}km/h pos={transform.position}");
            }
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
        //   Gear state machine
        // ============================================================

        // Transiciones:
        //   Drive    + parado + freno mantenido  -> ReverseArming (inicia timer)
        //   ReverseArming + freno liberado       -> Neutral (cancela)
        //   ReverseArming + timer expira         -> Reverse
        //   Reverse  + parado + freno mantenido  -> DriveArming simetrico via el mismo flag
        //   (por simplicidad reutilizamos ReverseArming para D->R y tratamos R->D igual,
        //    diferenciando con el gear de origen)
        //
        // Simplificacion: el timer de 2s es simetrico. Mientras se arma, el gear activo para
        // motor sigue siendo el anterior (no se aplica torque reversa hasta que termine el timer).
        private void UpdateGear(float throttle, float brake)
        {
            bool stopped = currentSpeed < stoppedThresholdKmh;
            bool brakeHeld = brake >= gearSwitchBrakeThreshold;
            bool throttlePressed = throttle > 0.05f;

            // Edge: al pasar de moviendo -> parado, exigir que el freno se suelte al menos
            // una vez antes de poder armar. Asi el freno que se venia sosteniendo para frenar
            // no cuenta; hay que estar quieto y recien ahi re-apretar el gatillo.
            if (stopped && !wasStoppedLastFrame) brakeReleaseRequired = true;
            wasStoppedLastFrame = stopped;

            if (!brakeHeld) brakeReleaseRequired = false;
            bool canArm = stopped && brakeHeld && !brakeReleaseRequired;

            switch (currentGear)
            {
                case GearState.Drive:
                    if (canArm)
                    {
                        gearSwitchTimer = 0f;
                        SetGear(GearState.ReverseArming);
                    }
                    break;

                case GearState.Reverse:
                    if (canArm)
                    {
                        gearSwitchTimer = 0f;
                        SetGear(GearState.DriveArming);
                    }
                    break;

                case GearState.ReverseArming:
                    if (throttlePressed)
                    {
                        gearSwitchTimer = 0f;
                        SetGear(GearState.Drive);
                        break;
                    }
                    if (!stopped || !brakeHeld)
                    {
                        gearSwitchTimer = 0f;
                        SetGear(GearState.Drive);
                        break;
                    }
                    gearSwitchTimer += Time.fixedDeltaTime;
                    if (gearSwitchTimer >= gearSwitchHoldSeconds)
                    {
                        gearSwitchTimer = 0f;
                        brakeReleaseRequired = true;
                        SetGear(GearState.Reverse);
                    }
                    break;

                case GearState.DriveArming:
                    if (throttlePressed)
                    {
                        gearSwitchTimer = 0f;
                        SetGear(GearState.Reverse);
                        break;
                    }
                    if (!stopped || !brakeHeld)
                    {
                        gearSwitchTimer = 0f;
                        SetGear(GearState.Reverse);
                        break;
                    }
                    gearSwitchTimer += Time.fixedDeltaTime;
                    if (gearSwitchTimer >= gearSwitchHoldSeconds)
                    {
                        gearSwitchTimer = 0f;
                        brakeReleaseRequired = true;
                        SetGear(GearState.Drive);
                    }
                    break;
            }
        }

        private void SetGear(GearState next)
        {
            if (next == currentGear) return;
            currentGear = next;
            EventBus.Dispatch_GearChanged(currentGear);
        }

        // ============================================================
        //   Helpers WheelCollider
        // ============================================================

        private void ApplyMotor(float input)
        {
            // RWD: traccion trasera. En D empuja adelante, en R empuja atras con cap bajo.
            // En Neutral/ReverseArming el motor queda libre (el freno, si lo hay, lo detiene).
            float torque = 0f;
            switch (currentGear)
            {
                case GearState.Drive:
                    if (currentSpeed < maxSpeed) torque = input * acceleration * 100f;
                    break;
                case GearState.Reverse:
                    if (currentSpeed < maxReverseSpeedKmh) torque = -input * acceleration * 100f;
                    break;
            }
            if (wheelRL != null) wheelRL.motorTorque = torque;
            if (wheelRR != null) wheelRR.motorTorque = torque;
        }

        private void ApplySteering(float input)
        {
            // Negamos para que input positivo (volante CW = derecha en EventBus) resulte en
            // giro del auto hacia la derecha con las delanteras. El signo de WheelCollider.steerAngle
            // en este rig queda cruzado respecto a la convencion del volante.
            float angle = -input * steerAngle;
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
            SyncWheel(wheelFL, wheelMeshFL, meshOffsetFL);
            SyncWheel(wheelFR, wheelMeshFR, meshOffsetFR);
            SyncWheel(wheelRL, wheelMeshRL, meshOffsetRL);
            SyncWheel(wheelRR, wheelMeshRR, meshOffsetRR);
        }

        private static Quaternion CaptureMeshOffset(WheelCollider col, Transform mesh)
        {
            if (col == null || mesh == null) return Quaternion.identity;
            col.GetWorldPose(out _, out Quaternion colRot);
            return Quaternion.Inverse(colRot) * mesh.rotation;
        }

        private static void SyncWheel(WheelCollider col, Transform mesh, Quaternion offset)
        {
            if (col == null || mesh == null) return;
            col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            mesh.SetPositionAndRotation(pos, rot * offset);
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
