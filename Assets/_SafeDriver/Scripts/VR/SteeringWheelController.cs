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
        [SerializeField] private float returnSpeed = 180f;

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
        /// </summary>
        private float ReadAngle()
        {
            Quaternion delta = Quaternion.Inverse(originalLocalRotation) * transform.localRotation;
            Vector3 euler = delta.eulerAngles;
            float a = euler[rotationAxis];
            if (a > 180f) a -= 360f;
            // Invertir para que giro horario visual del volante = steering positivo (derecha).
            return -a;
        }

        private void ReturnToCenter()
        {
            float currentAngle = ReadAngle();
            if (Mathf.Abs(currentAngle) < 0.5f) return;

            float targetAngle = Mathf.MoveTowards(currentAngle, 0f, returnSpeed * Time.deltaTime);
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
