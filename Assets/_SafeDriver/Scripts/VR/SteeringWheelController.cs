using System.Reflection;
using UnityEngine;
using Oculus.Interaction;
using SafeDriver.Core;
using SafeDriver.Vehicle;

namespace SafeDriver.VR
{
    /// <summary>
    /// Lector de rotacion del volante integrado con Interaction SDK.
    ///
    /// Responsabilidades:
    ///   - NO maneja grab ni snap de manos: eso lo hace ISDK (Grabbable + HandGrab/GrabInteractable).
    ///   - NO rota el volante mientras esta agarrado: eso lo hace OneGrabRotateTransformer.
    ///   - SI aplica return-to-center cuando se suelta.
    ///   - SI lee el angulo actual y lo empuja a VehicleInput y EventBus.
    ///
    /// Setup esperado:
    ///   - Grabbable / OneGrabRotateTransformer / GrabInteractable / HandGrabInteractable
    ///     con sus _rigidbody apuntando al Rigidbody del auto padre (NO uno propio).
    ///   - Collider (no trigger) — ISDK lo usa para detectar hover/grab; al no tener
    ///     Rigidbody propio, el collider pertenece al Rigidbody del auto (compound).
    ///   - Sin Rigidbody propio: evita el bug de nested Rigidbodies que causaba
    ///     despegue/jitter cuando el auto se movia por fisica.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class SteeringWheelController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float maxSteeringAngle = 180f;
        [Tooltip("Velocidad de retorno maxima del volante (deg/s), alcanzada cuando el auto va a referenceSpeedKmh o mas.")]
        [SerializeField] private float returnSpeed = 180f;
        [Tooltip("Velocidad del auto (km/h) a la que el recentrado del volante es maximo. A 0 km/h no hay recentrado (queda donde se dejo).")]
        [SerializeField] private float referenceSpeedKmh = 30f;

        [Header("Eje visual (0=X, 1=Y, 2=Z). Debe coincidir con OneGrabRotateTransformer.")]
        [SerializeField] private int rotationAxis = 1;

        [Header("Referencias ISDK")]
        [Tooltip("Grabbable del volante. Si queda vacio se busca en este GameObject.")]
        [SerializeField] private Grabbable grabbable;
        [Tooltip("Transformer que rota el volante. Si queda vacio se busca en este GameObject.")]
        [SerializeField] private OneGrabRotateTransformer rotateTransformer;

        [Header("Debug")]
        [SerializeField] private bool logInputs = false;

        private Quaternion originalLocalRotation;
        private float lastLogTime;

        // Reflection cache: OneGrabRotateTransformer guarda su angulo en campos privados que
        // persisten entre grabs. Al soltar y re-agarrar, el rango util queda recortado al
        // restante hasta MaxAngle. Sincronizamos estos campos con el angulo visual mientras
        // el volante NO esta agarrado para que la proxima BeginTransform parta desde el pose real.
        private static FieldInfo s_relativeAngleField;
        private static FieldInfo s_constrainedRelativeAngleField;

        void Awake()
        {
            originalLocalRotation = transform.localRotation;
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
            if (rotateTransformer == null) rotateTransformer = GetComponent<OneGrabRotateTransformer>();

            if (s_relativeAngleField == null)
            {
                var t = typeof(OneGrabRotateTransformer);
                const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
                s_relativeAngleField = t.GetField("_relativeAngle", flags);
                s_constrainedRelativeAngleField = t.GetField("_constrainedRelativeAngle", flags);
            }
        }

        void Update()
        {
            bool isGrabbed = grabbable != null && grabbable.SelectingPointsCount > 0;

            if (!isGrabbed)
            {
                ReturnToCenter();
                SyncTransformerAngleToVisual();
            }

            float currentAngle = ReadAngle();
            float normalized = Mathf.Clamp(currentAngle / maxSteeringAngle, -1f, 1f);

            if (VehicleInput.Instance != null)
                VehicleInput.Instance.SetSteering(normalized);
            EventBus.Dispatch_SteeringChanged(normalized);

            if (logInputs && Time.time - lastLogTime > 1f)
            {
                lastLogTime = Time.time;
                var vi = VehicleInput.Instance;
                string viState = vi == null
                    ? "VehicleInput.Instance == NULL"
                    : $"throttle={vi.ThrottleInput:F2} brake={vi.BrakeInput:F2} steer={vi.SteerInput:F2}";
                Debug.Log($"[WheelDebug] angle={currentAngle:F1}° grabbed={isGrabbed} | {viState}");
            }
        }

        /// <summary>
        /// Angulo firmado (-max..+max) sobre el eje configurado, relativo a la rotacion inicial.
        ///
        /// Lee directo de _constrainedRelativeAngle del transformer cuando esta disponible:
        /// es signed, unwrapped y ya clampado. Evita el wraparound de Quaternion.eulerAngles
        /// cerca del tope (±180°) que provocaba salto de steering al lado opuesto.
        /// </summary>
        private float ReadAngle()
        {
            if (rotateTransformer != null && s_constrainedRelativeAngleField != null)
            {
                float aField = (float)s_constrainedRelativeAngleField.GetValue(rotateTransformer);
                return -aField;
            }

            Quaternion delta = Quaternion.Inverse(originalLocalRotation) * transform.localRotation;
            Vector3 euler = delta.eulerAngles;
            float a = euler[rotationAxis];
            if (a > 180f) a -= 360f;
            // Invertir para que giro horario visual del volante = steering positivo (derecha).
            return -a;
        }

        /// <summary>
        /// Recentrado proporcional a la velocidad del auto: simula el self-aligning torque
        /// del caster + ackermann reales. A 0 km/h el volante se queda donde se dejo; a
        /// referenceSpeedKmh o mas, recentra a returnSpeed; entre medio, lineal.
        /// </summary>
        private void ReturnToCenter()
        {
            float currentAngle = ReadAngle();
            if (Mathf.Abs(currentAngle) < 0.5f) return;

            float speedKmh = VehicleController.Instance != null
                ? VehicleController.Instance.CurrentSpeedKmh
                : 0f;
            float speedFactor = Mathf.Clamp01(speedKmh / Mathf.Max(0.01f, referenceSpeedKmh));
            if (speedFactor <= 0.001f) return;

            float effectiveReturnSpeed = returnSpeed * speedFactor;
            float targetAngle = Mathf.MoveTowards(currentAngle, 0f, effectiveReturnSpeed * Time.deltaTime);
            Vector3 euler = Vector3.zero;
            euler[rotationAxis] = -targetAngle;
            transform.localRotation = originalLocalRotation * Quaternion.Euler(euler);
        }

        // Escribe el angulo visual crudo (pre-invert, en la convencion del transformer)
        // en los campos privados del OneGrabRotateTransformer para que la proxima BeginTransform
        // use _startAngle = angulo actual, restaurando el rango completo [MinAngle, MaxAngle].
        private void SyncTransformerAngleToVisual()
        {
            if (rotateTransformer == null || s_relativeAngleField == null) return;

            Quaternion delta = Quaternion.Inverse(originalLocalRotation) * transform.localRotation;
            Vector3 euler = delta.eulerAngles;
            float a = euler[rotationAxis];
            if (a > 180f) a -= 360f;

            s_relativeAngleField.SetValue(rotateTransformer, a);
            s_constrainedRelativeAngleField.SetValue(rotateTransformer, a);
        }
    }
}
