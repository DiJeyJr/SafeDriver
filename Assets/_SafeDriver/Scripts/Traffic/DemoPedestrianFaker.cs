using System.Collections;
using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Simulador minimal de peaton para demos sin NavMesh: cada N segundos notifica al
    /// PedestrianCrossingDetector que hay/no hay peaton presente, y mueve un visual entre
    /// dos puntos. Reemplazar por NPCPedestrianAI (requiere NavMesh bake) para uso real.
    /// </summary>
    public class DemoPedestrianFaker : MonoBehaviour
    {
        [Tooltip("Detector que recibe SetPedestriansPresent(bool).")]
        [SerializeField] private MonoBehaviour crossingNotifierRef;

        [Header("Visual peaton (opcional)")]
        [Tooltip("Transform del peaton visual que se mueve entre los waypoints.")]
        [SerializeField] private Transform pedestrianVisual;
        [SerializeField] private Transform waypointA;
        [SerializeField] private Transform waypointB;

        [Header("Tiempos (segundos)")]
        [SerializeField] private float idleSeconds = 4f;
        [SerializeField] private float crossingSeconds = 5f;

        [Header("Percepcion")]
        [Tooltip("Compuerta de la senda: el peaton espera a que NO haya vehiculos antes de bajar del cordon.")]
        [SerializeField] private CrosswalkTrafficGate trafficGate;

        private IPedestrianCrossingNotifier notifier;

        void Start()
        {
            notifier = crossingNotifierRef as IPedestrianCrossingNotifier;
            if (pedestrianVisual != null && waypointA != null)
                pedestrianVisual.position = waypointA.position;
            StartCoroutine(Loop());
        }

        private IEnumerator Loop()
        {
            bool toB = true;
            while (true)
            {
                yield return new WaitForSeconds(idleSeconds);

                // Mira antes de cruzar: espera en el cordon mientras haya un vehiculo en la senda.
                // Asi no se manda a cruzar cuando el player (o un NPC) esta pasando justo.
                while (trafficGate != null && !trafficGate.IsClear)
                    yield return null;

                notifier?.SetPedestriansPresent(true);
                yield return Animate(toB);
                notifier?.SetPedestriansPresent(false);

                toB = !toB;
            }
        }

        private IEnumerator Animate(bool toB)
        {
            if (pedestrianVisual == null || waypointA == null || waypointB == null)
            {
                yield return new WaitForSeconds(crossingSeconds);
                yield break;
            }

            Vector3 from = (toB ? waypointA : waypointB).position;
            Vector3 to   = (toB ? waypointB : waypointA).position;
            float t = 0f;
            while (t < crossingSeconds)
            {
                t += Time.deltaTime;
                pedestrianVisual.position = Vector3.Lerp(from, to, t / crossingSeconds);
                yield return null;
            }
            pedestrianVisual.position = to;
        }
    }
}
