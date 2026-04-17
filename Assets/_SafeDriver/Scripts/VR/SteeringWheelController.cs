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
    /// Setup esperado en el GameObject del volante:
    ///   - Rigidbody (kinematic)
    ///   - Grabbable (referenciada abajo)
    ///   - OneGrabRotateTransformer con Axis = Up y Constraints Min=-maxSteeringAngle, Max=+maxSteeringAngle
    ///   - GrabInteractable (controllers) y/o HandGrabInteractable (hand tracking)
    ///   - Collider
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

        private Quaternion originalLocalRotation;

        void Awake()
        {
            originalLocalRotation = transform.localRotation;
            if (grabbable == null) grabbable = GetComponent<Grabbable>();
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
