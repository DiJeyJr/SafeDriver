using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Vehiculo NPC que circula por waypoints y respeta semaforos.
    /// Para el nivel de demo: movimiento simple sin NavMesh ni WheelColliders.
    /// Logica: avanzar hacia el siguiente waypoint, rotar suavemente, frenar en rojo.
    ///
    /// Setup:
    ///   1. Crear Transforms vacios como waypoints a lo largo de la ruta
    ///   2. Asignar route[] en Inspector (en orden de recorrido, loopea al final)
    ///   3. Asignar nearestLight al semaforo que controla el paso de este NPC
    ///   4. stoppingDistance: distancia al semaforo donde empieza a frenar
    /// </summary>
    public class NPCVehicleAI : MonoBehaviour
    {
        [Header("Ruta")]
        [Tooltip("Waypoints de la ruta en orden. Al llegar al ultimo vuelve al primero (loop).")]
        public Transform[] route;

        [Header("Velocidad")]
        [Tooltip("Velocidad crucero en km/h.")]
        public float speed = 30f;

        [Tooltip("Suavizado de rotacion (mayor = giro mas suave).")]
        [SerializeField] private float rotationSmooth = 5f;

        [Header("Semaforo")]
        [Tooltip("Semaforo que controla el paso de este NPC. Puede ser null (sin semaforo).")]
        public TrafficLightController nearestLight;

        [Tooltip("Distancia al waypoint del semaforo donde el NPC frena si esta en rojo.")]
        [SerializeField] private float stoppingDistance = 5f;

        [Tooltip("Indice del waypoint del route[] que esta justo en la linea de detencion del semaforo.")]
        [SerializeField] private int lightWaypointIndex = 0;

        [Header("Config")]
        [Tooltip("Distancia al waypoint para considerar que llego.")]
        [SerializeField] private float arrivalThreshold = 0.5f;

        private int currentTarget;
        private bool isStopped;

        void Start()
        {
            currentTarget = 0;
        }

        void Update()
        {
            if (route == null || route.Length == 0) return;

            // Evaluar si debe frenar por semaforo en rojo
            if (ShouldStopForLight())
            {
                isStopped = true;
                return;
            }
            isStopped = false;

            MoveTowardsTarget();
        }

        private bool ShouldStopForLight()
        {
            if (nearestLight == null) return false;
            if (!nearestLight.IsRed() && !nearestLight.IsYellow()) return false;

            // Solo frenar si estamos cerca del waypoint del semaforo y no lo pasamos aun
            if (lightWaypointIndex < 0 || lightWaypointIndex >= route.Length) return false;

            // Chequeamos distancia a la linea de detencion
            float distToStopLine = Vector3.Distance(
                transform.position,
                route[lightWaypointIndex].position);

            // Frenar si estamos acercándonos (dentro de stoppingDistance)
            // y el siguiente target es el waypoint del semaforo o anterior
            bool approaching = currentTarget <= lightWaypointIndex
                             || (currentTarget == 0 && lightWaypointIndex == route.Length - 1);

            return approaching && distToStopLine < stoppingDistance;
        }

        private void MoveTowardsTarget()
        {
            if (route[currentTarget] == null) return;

            Vector3 targetPos = route[currentTarget].position;
            Vector3 direction = (targetPos - transform.position).normalized;

            // Mover (speed en km/h -> m/s = /3.6)
            float speedMps = speed / 3.6f;
            transform.position += direction * speedMps * Time.deltaTime;

            // Rotar suavemente hacia el destino
            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, rotationSmooth * Time.deltaTime);
            }

            // Llegamos al waypoint?
            if (Vector3.Distance(transform.position, targetPos) < arrivalThreshold)
            {
                currentTarget = (currentTarget + 1) % route.Length;
            }
        }
    }
}
