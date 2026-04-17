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
    ///   - SI ignora colisiones entre su Rigidbody kinematic y los colliders del auto padre
    ///     (previene depenetration que despegaria el volante al moverse el auto).
    ///
    /// Setup esperado:
    ///   - Rigidbody (kinematic)
    ///   - Grabbable / OneGrabRotateTransformer / GrabInteractable / HandGrabInteractable
    ///   - Collider (no trigger — ISDK lo usa para detectar hover/grab)
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

        [Header("Debug")]
        [SerializeField] private bool logInputs = false;

        private Quaternion originalLocalRotation;
        private Rigidbody ownRb;
        private float lastLogTime;

        void Awake()
        {
            originalLocalRotation = transform.localRotation;
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
            ownRb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            // Ignorar colisiones entre este Rigidbody kinematic y los colliders del auto padre.
            // Sin esto, la depenetration entre el sphere collider del volante y el box collider
            // del chassis despegaria el volante cuando el auto se mueve.
            IgnoreCollisionsWithParentRigidbody();
        }

        void FixedUpdate()
        {
            // Sincroniza fisicamente el Rigidbody kinematic con su transform (que sigue la jerarquia).
            // Sin esto, el Rigidbody del volante se queda en la pose world anterior cuando el auto se mueve,
            // aunque la transform jerarquica SI este actualizada. Clasico nested-Rigidbody issue.
            if (ownRb != null && ownRb.isKinematic)
            {
                ownRb.position = transform.position;
                ownRb.rotation = transform.rotation;
            }
        }

        void Update()
        {
            bool isGrabbed = grabbable != null && grabbable.SelectingPointsCount > 0;

            if (!isGrabbed)
                ReturnToCenter();

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

        private void IgnoreCollisionsWithParentRigidbody()
        {
            if (ownRb == null) return;
            var parentRb = GetComponentInParent<Rigidbody>();
            // Si el parent Rigidbody es el propio, subir un nivel.
            if (parentRb == ownRb)
            {
                var parent = transform.parent;
                parentRb = parent != null ? parent.GetComponentInParent<Rigidbody>() : null;
            }
            if (parentRb == null) return;

            var ownCols = GetComponentsInChildren<Collider>(true);
            var parentCols = parentRb.GetComponentsInChildren<Collider>(true);
            foreach (var a in ownCols)
            {
                if (a == null || a.attachedRigidbody != ownRb) continue;
                foreach (var b in parentCols)
                {
                    if (b == null || b.attachedRigidbody != parentRb) continue;
                    Physics.IgnoreCollision(a, b, true);
                }
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
    }
}
