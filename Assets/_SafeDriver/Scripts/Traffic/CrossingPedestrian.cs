using System.Collections;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Peaton de senda peatonal con el ciclo del diseño original (NPCPedestrianAI):
    /// espera en la vereda un tiempo random -> cruza por los waypoints -> espera del
    /// otro lado -> vuelve a cruzar. Mientras recorre el rango de waypoints de la cebra
    /// notifica al PedestrianCrossingDetector (asi el scoring evalua ceder el paso).
    ///
    /// Movimiento simple por lerp, SIN NavMesh: el kit del peaton funciona en cualquier
    /// nivel sin bakear nada. El atropello lo maneja el PedestrianHitbox del hijo.
    /// </summary>
    public class CrossingPedestrian : MonoBehaviour
    {
        [Header("Path (waypoints del cruce, de vereda a vereda)")]
        [SerializeField] private TrafficWaypointPath path;

        [Header("Ciclo")]
        [Tooltip("Espera minima en la vereda antes de volver a cruzar.")]
        [SerializeField] private float idleMinSeconds = 2f;

        [Tooltip("Espera maxima en la vereda antes de volver a cruzar.")]
        [SerializeField] private float idleMaxSeconds = 8f;

        [Header("Movimiento")]
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float arriveThreshold = 0.25f;
        [SerializeField] private float turnSpeed = 360f;

        [Header("Senda peatonal")]
        [Tooltip("Indice minimo del rango de waypoints dentro de la cebra.")]
        [SerializeField] private int crosswalkMinIndex = 1;

        [Tooltip("Indice maximo del rango de waypoints dentro de la cebra. Inclusivo.")]
        [SerializeField] private int crosswalkMaxIndex = 2;

        [Tooltip("PedestrianCrossingDetector (o cualquier IPedestrianCrossingNotifier).")]
        [SerializeField] private MonoBehaviour crossingNotifierRef;

        private int currentIndex;
        private int direction = 1; // 1 = ida, -1 = vuelta
        private bool crossing;
        private IPedestrianCrossingNotifier notifier;
        private IPedestrianCrossingMultiNotifier multiNotifier;
        private int notifierId;
        private bool inCrosswalk;

        void Start()
        {
            if (path == null || path.Count < 2) { enabled = false; return; }

            notifier = crossingNotifierRef as IPedestrianCrossingNotifier;
            multiNotifier = crossingNotifierRef as IPedestrianCrossingMultiNotifier;
            notifierId = GetInstanceID();

            // Arranca parado en la vereda (primer waypoint).
            currentIndex = 0;
            transform.position = path.GetPosition(0);
            StartCoroutine(IdleThenCross());
        }

        void OnDisable()
        {
            if (inCrosswalk) NotifyPresence(false);
            inCrosswalk = false;
        }

        private IEnumerator IdleThenCross()
        {
            crossing = false;
            yield return new WaitForSeconds(Random.Range(idleMinSeconds, idleMaxSeconds));
            crossing = true;
            currentIndex += direction;
            UpdateCrosswalkState();
        }

        void Update()
        {
            if (!crossing) return;

            Vector3 target = path.GetPosition(currentIndex);
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist < arriveThreshold)
            {
                bool llegoAlFinal = (direction > 0 && currentIndex >= path.Count - 1)
                                 || (direction < 0 && currentIndex <= 0);
                if (llegoAlFinal)
                {
                    // Cruce terminado: dar vuelta para la proxima y descansar en la vereda.
                    direction = -direction;
                    if (inCrosswalk) { inCrosswalk = false; NotifyPresence(false); }
                    StartCoroutine(IdleThenCross());
                    return;
                }

                currentIndex += direction;
                UpdateCrosswalkState();
                return;
            }

            Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        // Presencia en la cebra segun el waypoint al que se dirige (mismo criterio que
        // TrafficPedestrian: avisa apenas encara el tramo de cebra — conservador y seguro).
        private void UpdateCrosswalkState()
        {
            if (crosswalkMinIndex < 0 || crosswalkMaxIndex < crosswalkMinIndex) return;

            bool nowInside = currentIndex >= crosswalkMinIndex && currentIndex <= crosswalkMaxIndex;
            if (nowInside == inCrosswalk) return;

            inCrosswalk = nowInside;
            NotifyPresence(nowInside);
        }

        private void NotifyPresence(bool present)
        {
            if (multiNotifier != null) multiNotifier.NotifyByInstance(notifierId, present);
            else notifier?.SetPedestriansPresent(present);
        }
    }
}
