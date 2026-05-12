using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Peaton ambiental que recorre un path en loop o rebote. Movimiento simple (lerp +
    /// rotacion), sin NavMesh ni fisica.
    ///
    /// Si el path pasa por una zona de cebra, los waypoints contenidos en el rango
    /// [crosswalkMinIndex, crosswalkMaxIndex] cuentan como "dentro de la senda". Mientras
    /// el peaton este en ese rango, notifica al `PedestrianCrossingDetector` para que el
    /// scoring sepa que hay peatones (en cualquiera de los dos sentidos de marcha — esto
    /// funciona con paths abiertos en modo rebote).
    ///
    /// Usa la API `NotifyByInstance(id, present)` del detector cuando esta disponible,
    /// asi multiples peatones pueden coexistir sin pisarse el bool. Cae al
    /// `SetPedestriansPresent(bool)` clasico si el detector no lo expone.
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
        [Tooltip("Indice minimo del rango de waypoints contenidos en la cebra. -1 = no notificar.")]
        [SerializeField] private int crosswalkMinIndex = -1;

        [Tooltip("Indice maximo del rango de waypoints contenidos en la cebra. Inclusivo.")]
        [SerializeField] private int crosswalkMaxIndex = -1;

        [Tooltip("PedestrianCrossingDetector (u otro componente que implemente IPedestrianCrossingNotifier).")]
        [SerializeField] private MonoBehaviour crossingNotifierRef;

        [Header("Player")]
        [SerializeField] private string playerTag = "PlayerVehicle";

        private int currentIndex;
        private int direction = 1;
        private IPedestrianCrossingNotifier notifier;
        private IPedestrianCrossingMultiNotifier multiNotifier;
        private bool inCrosswalk;
        private int notifierId;

        void Start()
        {
            if (path == null) { enabled = false; return; }
            currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, path.Count - 1));

            notifier = crossingNotifierRef as IPedestrianCrossingNotifier;
            multiNotifier = crossingNotifierRef as IPedestrianCrossingMultiNotifier;
            notifierId = GetInstanceID();

            // Asegurar el estado inicial coherente con el currentIndex
            UpdateCrosswalkStateFor(currentIndex);
        }

        void OnDisable()
        {
            // Si nos apagamos estando en la cebra, sacar el flag asi no queda residuo
            if (inCrosswalk) NotifyPresence(false);
            inCrosswalk = false;
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
                path.Advance(ref currentIndex, ref direction);
                UpdateCrosswalkStateFor(currentIndex);
                target = path.GetPosition(currentIndex);
                toTarget = target - transform.position;
                toTarget.y = 0f;
                dist = toTarget.magnitude;
                if (dist < 0.001f) return;
            }

            Quaternion lookRot = Quaternion.LookRotation(toTarget.normalized);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
            transform.position += transform.forward * speed * Time.deltaTime;
        }

        /// <summary>
        /// Actualiza el estado de presencia en la cebra segun el waypoint actual. Si el indice
        /// cae dentro de [crosswalkMinIndex, crosswalkMaxIndex] se notifica present=true; fuera,
        /// present=false. Funciona bidireccional (al rebotar el path).
        /// </summary>
        private void UpdateCrosswalkStateFor(int index)
        {
            if (crosswalkMinIndex < 0 || crosswalkMaxIndex < crosswalkMinIndex) return;

            bool nowInside = index >= crosswalkMinIndex && index <= crosswalkMaxIndex;
            if (nowInside == inCrosswalk) return;

            inCrosswalk = nowInside;
            NotifyPresence(nowInside);
        }

        private void NotifyPresence(bool present)
        {
            if (multiNotifier != null) multiNotifier.NotifyByInstance(notifierId, present);
            else notifier?.SetPedestriansPresent(present);
        }

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            EventBus.Dispatch_Infraction(
                InfractionType.PedestrianNotYielded,
                "Atropello a un peaton. Disminuir velocidad cerca de cebras y veredas.");
        }
    }
}
