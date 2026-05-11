using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Auto NPC que sigue un TrafficWaypointPath. Avanza con velocidad constante, frena
    /// suavemente al detectar al jugador adelante (raycast), rota para mirar al proximo
    /// waypoint, y dispara una infraccion grave si el jugador lo embiste (collision con
    /// el BoxCollider trigger del NPC).
    ///
    /// Sin Rigidbody — controlado solo por scripts. El BoxCollider esta en isTrigger=true
    /// para que el auto del jugador no rebote contra el (sin fisica) pero igual dispare
    /// el OnTriggerEnter para detectar la colision logica.
    /// </summary>
    public class TrafficVehicle : MonoBehaviour
    {
        [Header("Path")]
        [Tooltip("Path de waypoints a seguir.")]
        [SerializeField] private TrafficWaypointPath path;

        [Tooltip("Indice de waypoint inicial (modulo Count).")]
        [SerializeField] private int startIndex = 0;

        [Header("Movimiento")]
        [Tooltip("Velocidad de crucero (m/s).")]
        [SerializeField] private float cruiseSpeed = 6f;

        [Tooltip("Aceleracion / deceleracion (m/s^2).")]
        [SerializeField] private float accel = 6f;

        [Tooltip("Velocidad angular (grados/s) al rotar hacia el proximo waypoint.")]
        [SerializeField] private float turnSpeed = 180f;

        [Tooltip("Distancia (m) al waypoint para considerarlo alcanzado.")]
        [SerializeField] private float arriveThreshold = 0.6f;

        [Header("Safety")]
        [Tooltip("Distancia (m) hacia adelante para chequear si el jugador esta atravesado.")]
        [SerializeField] private float forwardCheckDistance = 5f;

        [Tooltip("Tag del jugador (para la colision logica).")]
        [SerializeField] private string playerTag = "PlayerVehicle";

        [Header("Audio (opcional)")]
        [SerializeField] private AudioSource horn;

        private int currentIndex;
        private int direction = 1;
        private float currentSpeed;

        void Start()
        {
            if (path == null) { enabled = false; return; }
            currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, path.Count - 1));
            currentSpeed = cruiseSpeed;

            // Orientar al primer waypoint
            var first = path.GetPosition(currentIndex);
            var dir = (first - transform.position);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        }

        void Update()
        {
            if (path == null || path.Count == 0) return;

            // Frenar si hay jugador adelante (raycast)
            float targetSpeed = IsPlayerAhead() ? 0f : cruiseSpeed;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);

            // Direccion hacia el waypoint actual
            Vector3 target = path.GetPosition(currentIndex);
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist < arriveThreshold)
            {
                path.Advance(ref currentIndex, ref direction);
                target = path.GetPosition(currentIndex);
                toTarget = target - transform.position;
                toTarget.y = 0f;
                dist = toTarget.magnitude;
                if (dist < 0.001f) return;
            }

            // Rotar hacia el waypoint
            Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, turnSpeed * Time.deltaTime);

            // Avanzar adelante (eje Z local)
            transform.position += transform.forward * currentSpeed * Time.deltaTime;
        }

        private bool IsPlayerAhead()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            // Raycast al frente
            if (Physics.Raycast(origin, transform.forward, out RaycastHit hit, forwardCheckDistance))
            {
                if (hit.collider != null && hit.collider.CompareTag(playerTag)) return true;
            }
            return false;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            // El jugador embistio al auto NPC — infraccion grave
            EventBus.Dispatch_InfractionDetected(
                InfractionType.DangerousManeuver,
                "Choque con vehiculo. Mantener distancia y respetar el carril.");
            if (horn != null) horn.Play();
        }
    }
}
