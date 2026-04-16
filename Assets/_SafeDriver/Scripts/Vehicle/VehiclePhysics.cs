using UnityEngine;

namespace SafeDriver.Vehicle
{
    /// <summary>
    /// Companion de VehicleController que ajusta parametros fisicos del Rigidbody al iniciar:
    /// - Baja el centro de masa para que no vuelque en curvas rapidas
    /// - (opcional) downforce a alta velocidad para estabilidad
    /// - Exposicion de metricas derivadas (RPM estimada, forceForward) para otras capas
    ///
    /// NO aplica input ni toca WheelColliders — eso lo hace VehicleController.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class VehiclePhysics : MonoBehaviour
    {
        [Header("Centro de masa")]
        [Tooltip("Offset del center of mass respecto al pivot del objeto, en metros locales.")]
        [SerializeField] private Vector3 centerOfMassOffset = new Vector3(0f, -0.4f, 0f);

        [Header("Downforce")]
        [Tooltip("Fuerza vertical aplicada hacia abajo proporcional a la velocidad. 0 = desactivado.")]
        [SerializeField] private float downforceCoefficient = 0f;

        private Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.centerOfMass = centerOfMassOffset;
            }
        }

        void FixedUpdate()
        {
            if (rb == null || downforceCoefficient <= 0f) return;
            float speed = rb.linearVelocity.magnitude;
            rb.AddForce(-transform.up * (downforceCoefficient * speed), ForceMode.Force);
        }

        /// <summary>Estima un RPM aproximado basado en velocidad angular de la rueda trasera izq.</summary>
        public float EstimateRpmFromWheel(WheelCollider wheel)
        {
            if (wheel == null) return 0f;
            return Mathf.Abs(wheel.rpm);
        }
    }
}
