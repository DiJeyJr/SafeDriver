using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Peaton ambiental que recorre un path en loop (por las veredas tipicamente).
    /// Si el path pasa por una zona de paso peatonal y el NPC entra/sale de ella, notifica
    /// al detector via IPedestrianCrossingNotifier — asi el scoring sabe que hay peatones
    /// presentes en la senda y puede premiar al jugador que ceda o sancionar al que no.
    ///
    /// Versionado liviano: sin NavMesh, sin Rigidbody, sin colisiones fisicas. Solo lerp
    /// entre waypoints y rotacion para mirar adelante. Si el jugador lo embiste, dispara
    /// infraccion grave igual que TrafficVehicle.
    /// </summary>
    public class TrafficPedestrian : MonoBehaviour
    {
        [Header("Path")]
        [SerializeField] private TrafficWaypointPath path;
        [SerializeField] private int startIndex = 0;

        [Header("Movimiento")]
        [SerializeField] private float speed = 1.3f;
        [SerializeField] private float arriveThreshold = 0.25f;
        [SerializeField] private float turnSpeed = 360f;

        [Header("Senda peatonal (opcional)")]
        [Tooltip("Indice del waypoint donde el peaton ENTRA a la senda (notifica present=true).")]
        [SerializeField] private int crosswalkEnterIndex = -1;

        [Tooltip("Indice del waypoint donde el peaton SALE de la senda (notifica present=false).")]
        [SerializeField] private int crosswalkExitIndex = -1;

        [Tooltip("Componente que implementa IPedestrianCrossingNotifier (tipicamente el PedestrianCrossingDetector).")]
        [SerializeField] private MonoBehaviour crossingNotifierRef;

        [Header("Player")]
        [SerializeField] private string playerTag = "PlayerVehicle";

        private int currentIndex;
        private int direction = 1;
        private IPedestrianCrossingNotifier notifier;
        private bool inCrosswalk;

        void Start()
        {
            if (path == null) { enabled = false; return; }
            currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, path.Count - 1));
            notifier = crossingNotifierRef as IPedestrianCrossingNotifier;
        }

        void Update()
        {
            if (path == null || path.Count == 0) return;

            Vector3 target = path.GetPosition(currentIndex);
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            if (dist < arriveThreshold)
            {
                UpdateCrosswalkState(currentIndex);
                path.Advance(ref currentIndex, ref direction);
                target = path.GetPosition(currentIndex);
                toTarget = target - transform.position;
                toTarget.y = 0f;
                dist = toTarget.magnitude;
                if (dist < 0.001f) return;
            }

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
                transform.position += transform.forward * speed * Time.deltaTime;
            }
        }

        private void UpdateCrosswalkState(int reachedIndex)
        {
            if (notifier == null) return;
            if (reachedIndex == crosswalkEnterIndex && !inCrosswalk)
            {
                inCrosswalk = true;
                notifier.SetPedestriansPresent(true);
            }
            else if (reachedIndex == crosswalkExitIndex && inCrosswalk)
            {
                inCrosswalk = false;
                notifier.SetPedestriansPresent(false);
            }
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            EventBus.Dispatch_InfractionDetected(
                InfractionType.PedestrianNotYielded,
                "Atropelló a un peaton. Disminuir velocidad cerca de cebras y veredas.");
        }
    }
}
