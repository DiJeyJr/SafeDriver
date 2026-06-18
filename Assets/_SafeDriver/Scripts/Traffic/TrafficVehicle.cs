using UnityEngine;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Auto NPC que sigue un TrafficWaypointPath. Avanza a velocidad de crucero y frena suave ante:
    ///   - un obstaculo adelante (el player, otro NPC o un peaton cruzando) — SphereCast,
    ///   - una linea de detencion (TrafficStopLine) de un semaforo en rojo — por distancia.
    /// Frena para parar JUSTO antes de la linea; si ya la paso, termina de cruzar (no se queda en el medio).
    ///
    /// MOVIMIENTO: Rigidbody kinematic movido con MovePosition/MoveRotation en FixedUpdate +
    /// interpolation=Interpolate. Esto da velocidad CONSISTENTE (timestep fijo, no depende del framerate
    /// de render, importante en VR) y a la vez se ve suave (la interpolacion rellena entre pasos de physics).
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
        [Tooltip("Distancia (m) hacia adelante para chequear obstaculos (player/NPC/peaton).")]
        [SerializeField] private float forwardCheckDistance = 5f;

        [Tooltip("Distancia (m) a la que empieza a considerar una linea de stop en rojo.")]
        [SerializeField] private float stopLineRange = 12f;

        [Tooltip("Cuanto antes de la linea para (m). Mas alto = frena con mas margen antes de la senda.")]
        [SerializeField] private float stopLineOffset = 2.5f;

        [Tooltip("Tag del jugador.")]
        [SerializeField] private string playerTag = "PlayerVehicle";

        [Tooltip("Tag del peaton (para frenar si esta cruzando adelante).")]
        [SerializeField] private string pedestrianTag = "Pedestrian";

        private int currentIndex;
        private int direction = 1;
        private float currentSpeed;
        private TrafficStopLine[] stopLines;
        private Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // suave entre pasos de physics
        }

        void Start()
        {
            if (path == null) { enabled = false; return; }
            currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, path.Count - 1));
            currentSpeed = cruiseSpeed;
            stopLines = Object.FindObjectsByType<TrafficStopLine>(FindObjectsSortMode.None);

            var first = path.GetPosition(currentIndex);
            var dir = (first - transform.position);
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z));
        }

        // En FixedUpdate (timestep fijo) para velocidad consistente, frame-rate independiente.
        void FixedUpdate()
        {
            if (path == null || path.Count == 0) return;
            float dt = Time.fixedDeltaTime;

            // Velocidad objetivo: crucero, salvo obstaculo adelante (para) o linea de stop roja (desacelera).
            float targetSpeed = IsObstacleAhead() ? 0f : cruiseSpeed;

            float stopAhead = NearestRedStopLineAhead();
            if (stopAhead < stopLineRange)
            {
                // Velocidad maxima para frenar a 'accel' y parar 'stopLineOffset' antes de la linea.
                float d = Mathf.Max(0f, stopAhead - stopLineOffset);
                float vMax = Mathf.Sqrt(2f * accel * d);
                if (vMax < targetSpeed) targetSpeed = vMax;
            }

            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * dt);

            Vector3 target = path.GetPosition(currentIndex);
            Vector3 toTarget = target - rb.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist < arriveThreshold)
            {
                path.Advance(ref currentIndex, ref direction);
                target = path.GetPosition(currentIndex);
                toTarget = target - rb.position;
                toTarget.y = 0f;
                dist = toTarget.magnitude;
                if (dist < 0.001f) return;
            }

            Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
            Quaternion newRot = Quaternion.RotateTowards(rb.rotation, lookRot, turnSpeed * dt);
            rb.MoveRotation(newRot);
            Vector3 forward = newRot * Vector3.forward;
            rb.MovePosition(rb.position + forward * currentSpeed * dt);
        }

        // Distancia (m) a la linea de stop ROJA mas cercana que tengo ADELANTE y en mi carril.
        // float.MaxValue si no hay ninguna. Si ya pase la linea (ahead<=0.5) la ignoro → termino de cruzar.
        private float NearestRedStopLineAhead()
        {
            float best = float.MaxValue;
            if (stopLines == null) return best;
            Vector3 pos = rb != null ? rb.position : transform.position;
            Vector3 fwd = transform.forward;
            Vector3 right = transform.right;
            foreach (var line in stopLines)
            {
                if (line == null || !line.IsStop) continue;
                Vector3 to = line.Position - pos;
                to.y = 0f;
                float ahead = Vector3.Dot(to, fwd);
                if (ahead <= 0.5f) continue;                          // ya la pase (o estoy encima)
                if (Mathf.Abs(Vector3.Dot(to, right)) > 2.5f) continue; // no esta en mi carril
                if (ahead < best) best = ahead;
            }
            return best;
        }

        // Obstaculo adelante (player/NPC/peaton) dentro de forwardCheckDistance → frenar. SphereCast (no
        // un rayo fino) para no fallar cosas descentradas; reconoce al player por el tag de su rigidbody.
        private bool IsObstacleAhead()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            var hits = Physics.SphereCastAll(origin, 0.9f, transform.forward, forwardCheckDistance,
                                             ~0, QueryTriggerInteraction.Collide);
            foreach (var hit in hits)
            {
                var col = hit.collider;
                if (col == null || col.transform.IsChildOf(transform)) continue;

                bool isPlayer = col.CompareTag(playerTag)
                             || (col.attachedRigidbody != null && col.attachedRigidbody.CompareTag(playerTag));
                var otherNpc = col.GetComponentInParent<TrafficVehicle>();
                bool isNpc = otherNpc != null && otherNpc != this;
                bool isPed = !string.IsNullOrEmpty(pedestrianTag) && col.CompareTag(pedestrianTag);

                if (isPlayer || isNpc || isPed) return true;
            }
            return false;
        }
    }
}
