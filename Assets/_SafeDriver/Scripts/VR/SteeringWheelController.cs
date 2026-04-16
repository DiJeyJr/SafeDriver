using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;

namespace SafeDriver.VR
{
    /// <summary>
    /// Controla la rotacion del volante con los controllers Quest (NO hand tracking).
    ///
    /// Mecanica:
    ///   - El jugador mantiene GRIP en cualquier controller (L o R o ambos)
    ///   - Mientras mantiene grip, la rotacion YAW del controller se mapea a la rotacion
    ///     del volante alrededor de su eje local Z
    ///   - Al soltar grip, el volante vuelve suavemente a centro (return-to-center)
    ///   - La rotacion normalizada (-1..+1) se envia a VehicleInput para mover el auto
    ///
    /// Setup:
    ///   1. Agregar este componente al GameObject del SteeringWheel
    ///   2. El script rota el transform del mismo objeto
    ///   3. VehicleInput.Instance recibe el steering automaticamente
    /// </summary>
    public class SteeringWheelController : MonoBehaviour
    {
        [Header("Config")]
        [Tooltip("Angulo maximo del volante en grados (un solo lado). 180 = media vuelta.")]
        [SerializeField] private float maxSteeringAngle = 180f;

        [Tooltip("Velocidad de retorno a centro cuando se suelta el grip (grados/seg).")]
        [SerializeField] private float returnSpeed = 180f;

        [Tooltip("Sensibilidad: cuantos grados de volante por grado de rotacion del controller.")]
        [SerializeField] private float sensitivity = 2.5f;

        [Tooltip("Zona muerta del controller en grados (evita drift).")]
        [SerializeField] private float deadZone = 3f;

        private float currentAngle; // grados acumulados del volante (-maxSteeringAngle..+maxSteeringAngle)
        private float grabStartYaw; // yaw del controller al momento del grab
        private float grabStartAngle; // angulo del volante al momento del grab
        private bool isGrabbed;
        private bool useLeftHand;
        private Quaternion originalLocalRotation; // rotacion original del mesh (inclinacion del tablero)

        void Start()
        {
            // Guardar la rotacion original del volante (incluye la inclinacion del tablero del FBX).
            // La rotacion de steering se aplica ENCIMA de esta como delta en Z local.
            originalLocalRotation = transform.localRotation;
        }

        void Update()
        {
            // Detectar grip press/release
            bool leftGrip  = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
            bool rightGrip = OVRInput.Get(OVRInput.Button.SecondaryHandTrigger);

            if (!isGrabbed && (leftGrip || rightGrip))
            {
                BeginGrab(leftGrip);
            }
            else if (isGrabbed && !leftGrip && !rightGrip)
            {
                EndGrab();
            }

            if (isGrabbed)
            {
                UpdateGrabbedRotation();
            }
            else
            {
                ReturnToCenter();
            }

            // Aplicar rotacion visual al mesh del volante
            ApplyVisualRotation();

            // Enviar a VehicleInput
            float normalized = currentAngle / maxSteeringAngle; // -1..+1
            if (VehicleInput.Instance != null)
                VehicleInput.Instance.SetSteering(normalized);

            // Publicar para el HUD (SpeedometerNeedle del volante si hay)
            EventBus.Dispatch_SteeringChanged(normalized);
        }

        private void BeginGrab(bool isLeft)
        {
            isGrabbed = true;
            useLeftHand = isLeft;
            grabStartYaw = GetControllerYaw();
            grabStartAngle = currentAngle;
        }

        private void EndGrab()
        {
            isGrabbed = false;
        }

        private void UpdateGrabbedRotation()
        {
            float currentYaw = GetControllerYaw();
            float delta = currentYaw - grabStartYaw;

            // Zona muerta
            if (Mathf.Abs(delta) < deadZone)
                delta = 0f;
            else
                delta -= Mathf.Sign(delta) * deadZone;

            // Aplicar sensibilidad y sumar al angulo base del grab
            float targetAngle = grabStartAngle + delta * sensitivity;
            currentAngle = Mathf.Clamp(targetAngle, -maxSteeringAngle, maxSteeringAngle);
        }

        private void ReturnToCenter()
        {
            // Volante vuelve a 0 suavemente (como un auto real con caster)
            if (Mathf.Abs(currentAngle) > 0.5f)
            {
                currentAngle = Mathf.MoveTowards(currentAngle, 0f, returnSpeed * Time.deltaTime);
            }
            else
            {
                currentAngle = 0f;
            }
        }

        [Header("Eje de rotacion visual")]
        [Tooltip("Eje local del mesh alrededor del cual gira el volante (columna de direccion). 0=X, 1=Y, 2=Z.")]
        [SerializeField] private int rotationAxis = 1; // Y por defecto (Blender Z-up → Unity Y)

        private void ApplyVisualRotation()
        {
            // Preservar la inclinacion original del volante (del FBX/tablero)
            // y agregar la rotacion de steering en el eje de la columna de direccion.
            Vector3 euler = Vector3.zero;
            euler[rotationAxis] = -currentAngle;
            transform.localRotation = originalLocalRotation * Quaternion.Euler(euler);
        }

        /// <summary>
        /// Lee el yaw (rotacion horizontal) del controller activo.
        /// Para el volante nos interesa la rotacion roll (Z) del controller,
        /// que es cuando el jugador gira la muneca como girando un volante.
        /// </summary>
        private float GetControllerYaw()
        {
            OVRInput.Controller ctrl = useLeftHand
                ? OVRInput.Controller.LTouch
                : OVRInput.Controller.RTouch;

            Quaternion rot = OVRInput.GetLocalControllerRotation(ctrl);
            // El roll del controller (Z axis) mapea a la rotacion del volante
            // Cuando el jugador gira la muneca clockwise, el roll cambia
            Vector3 euler = rot.eulerAngles;
            float roll = euler.z;
            if (roll > 180f) roll -= 360f;
            return roll;
        }
    }
}
