using UnityEngine;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Sensor que detecta el EVENTO de que el vehiculo cruza una linea, y en que
    /// direccion. Usa un BoxCollider trigger amplio (no una placa fina) para evitar
    /// el tunneling a velocidad alta con el paso de FixedUpdate.
    ///
    /// La linea se define por un origen (este transform) y una normal (lineNormal,
    /// la direccion "hacia adelante" del cruce legal). Se mide de que lado esta el
    /// auto con el signo de dot(posAuto - origen, normal); cuando el signo cambia
    /// mientras el auto esta dentro del trigger, ocurrio el cruce, y el signo final
    /// indica si fue hacia adelante (forward) o hacia atras.
    ///
    /// Las subclases implementan OnCrossed(forward, vehicle).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public abstract class CrossingLineSensor : VehicleSensor
    {
        [Header("Linea de cruce")]
        [Tooltip("Direccion local que cuenta como 'hacia adelante' al cruzar. Default: eje Z local.")]
        [SerializeField] private Vector3 localForward = Vector3.forward;

        // Lado del auto respecto a la linea en el frame anterior (-1, 0, +1). 0 = aun no medido.
        private int lastSide;
        private bool tracking;

        private Vector3 LineNormal => transform.TransformDirection(localForward.normalized);

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other)) return;
            tracking = true;
            lastSide = SideOf(PlayerPosition(other));
        }

        void OnTriggerStay(Collider other)
        {
            if (!tracking || !IsPlayer(other)) return;

            int side = SideOf(PlayerPosition(other));
            if (side != 0 && lastSide != 0 && side != lastSide)
            {
                // Cambio de lado => cruzo. side > 0 significa que termino del lado
                // de la normal (hacia adelante).
                bool forward = side > 0;
                OnCrossed(forward, Vehicle);
            }
            if (side != 0) lastSide = side;
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other)) return;
            tracking = false;
            lastSide = 0;
        }

        private int SideOf(Vector3 worldPos)
        {
            float d = Vector3.Dot(worldPos - transform.position, LineNormal);
            if (d > 0.01f) return 1;
            if (d < -0.01f) return -1;
            return 0;
        }

        // Usa el centro del Rigidbody del auto si esta disponible (mas estable que el
        // punto de contacto del collider compound).
        private static Vector3 PlayerPosition(Collider other)
        {
            var rb = other.attachedRigidbody;
            return rb != null ? rb.worldCenterOfMass : other.transform.position;
        }

        /// <summary>Se llama cuando el auto cruza la linea. forward = en el sentido de la normal.</summary>
        protected abstract void OnCrossed(bool forward, SafeDriver.Vehicle.VehicleController vehicle);

        void OnDrawGizmos()
        {
            Vector3 n = LineNormal;
            // Linea perpendicular a la normal (la "barrera" visual).
            Vector3 right = Vector3.Cross(n, Vector3.up).normalized;
            if (right.sqrMagnitude < 0.001f) right = transform.right;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position - right * 3f, transform.position + right * 3f);
            // Flecha de direccion legal.
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, transform.position + n * 2f);
        }
    }
}
