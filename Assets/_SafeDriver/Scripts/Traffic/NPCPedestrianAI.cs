using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Peaton NPC con NavMesh.
    /// Ciclo: Idle (2-8s random) -> BeginCrossing -> caminar por waypoints -> fin cruce -> repeat.
    /// Notifica al detector de cruce (via IPedestrianCrossingNotifier en Core)
    /// cuando pisa/deja la senda para que Scoring evalúe si el jugador cedio el paso.
    ///
    /// Requiere: NavMeshAgent en el mismo GameObject.
    /// Setup:
    ///   1. Bake NavMesh en la escena que cubra las sendas peatonales
    ///   2. Crear waypoints (Transforms vacios) en los extremos de la senda
    ///   3. Asignar waypoints[] y crossingNotifier en Inspector
    ///      (crossingNotifier = el GameObject con PedestrianCrossingDetector)
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class NPCPedestrianAI : MonoBehaviour
    {
        [Header("Waypoints de cruce")]
        [Tooltip("Puntos en orden que el peaton recorre al cruzar. El ultimo marca el fin del cruce.")]
        public Transform[] waypoints;

        [Header("Detector de senda")]
        [Tooltip("GameObject con componente que implementa IPedestrianCrossingNotifier (PedestrianCrossingDetector).")]
        [SerializeField] private MonoBehaviour crossingNotifierRef;

        [Header("Config")]
        [SerializeField] private float idleMinSeconds = 2f;
        [SerializeField] private float idleMaxSeconds = 8f;
        [SerializeField] private float walkSpeed = 1.4f;
        [SerializeField] private float arrivalThreshold = 0.3f;

        private NavMeshAgent agent;
        private IPedestrianCrossingNotifier crossingNotifier;
        private int currentWaypoint;
        private bool isCrossing;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.speed = walkSpeed;
            agent.stoppingDistance = arrivalThreshold * 0.5f;

            // Resolver interfaz desde la referencia del Inspector
            if (crossingNotifierRef != null)
                crossingNotifier = crossingNotifierRef as IPedestrianCrossingNotifier;

            currentWaypoint = 0;
            isCrossing = false;

            if (waypoints != null && waypoints.Length > 0)
                StartCoroutine(IdleThenCross());
        }

        private IEnumerator IdleThenCross()
        {
            float waitTime = Random.Range(idleMinSeconds, idleMaxSeconds);
            yield return new WaitForSeconds(waitTime);
            BeginCrossing();
        }

        private void BeginCrossing()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            isCrossing = true;
            currentWaypoint = 0;

            crossingNotifier?.SetPedestriansPresent(true);
            agent.SetDestination(waypoints[currentWaypoint].position);
        }

        void Update()
        {
            if (!isCrossing) return;
            if (agent == null || waypoints == null || waypoints.Length == 0) return;

            if (agent.remainingDistance < arrivalThreshold && !agent.pathPending)
            {
                currentWaypoint++;
                if (currentWaypoint >= waypoints.Length)
                {
                    FinishCrossing();
                }
                else
                {
                    agent.SetDestination(waypoints[currentWaypoint].position);
                }
            }
        }

        private void FinishCrossing()
        {
            isCrossing = false;
            currentWaypoint = 0;
            crossingNotifier?.SetPedestriansPresent(false);
            StartCoroutine(IdleThenCross());
        }
    }
}
