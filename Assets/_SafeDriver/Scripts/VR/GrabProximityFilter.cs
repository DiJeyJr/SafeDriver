using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

namespace SafeDriver.VR
{
    /// <summary>
    /// Filtro de proximidad generico para Grab/HandGrab Interactables.
    /// Limita el grab a interactors cuya posicion tracked este dentro (o cerca) del Collider hitbox.
    ///
    /// Necesario porque GrabInteractable auto-recolecta todos los colliders del Rigidbody asociado.
    /// Cuando un grabbable comparte el Rigidbody del chasis (compound), sin este filtro se podria
    /// agarrar desde cualquier collider del auto. Asignarlo al campo _interactorFilters del
    /// GrabInteractable y HandGrabInteractable.
    ///
    /// A diferencia de WheelGrabProximityFilter (que asume SphereCollider), este acepta cualquier
    /// tipo de Collider y usa ClosestPoint para medir distancia.
    /// </summary>
    public class GrabProximityFilter : MonoBehaviour, IGameObjectFilter
    {
        [Tooltip("Collider que define la zona de grab. Si queda vacio se usa el de este GameObject.")]
        [SerializeField] private Collider hitbox;

        [Tooltip("Padding (m) sumado al limite del collider. 0 = exactamente sobre el collider.")]
        [SerializeField] private float padding = 0.02f;

        [SerializeField] private bool logDebug = false;
        private float lastLog;

        void Awake()
        {
            if (hitbox == null) hitbox = GetComponent<Collider>();
        }

        public bool Filter(GameObject interactorGameObject)
        {
            if (hitbox == null) return true;

            Vector3 probe = ResolveTrackedPosition(interactorGameObject);
            Vector3 closest = hitbox.ClosestPoint(probe);
            float dist = Vector3.Distance(probe, closest);
            bool inside = dist <= padding;

            if (logDebug && Time.time - lastLog > 0.5f)
            {
                lastLog = Time.time;
                Debug.Log($"[GrabFilter:{name}] interactor={interactorGameObject.name} probe={probe} closest={closest} dist={dist:F3} inside={inside}");
            }
            return inside;
        }

        private static Vector3 ResolveTrackedPosition(GameObject interactorGO)
        {
            var hgi = interactorGO.GetComponent<HandGrabInteractor>();
            if (hgi != null)
            {
                // PalmPoint (centro de la palma) primero porque es donde la mano
                // realmente toca el objeto. WristPoint queda ~15cm detras y rechaza
                // grabs validos sobre objetos chicos.
                if (hgi.PalmPoint != null) return hgi.PalmPoint.position;
                if (hgi.WristPoint != null) return hgi.WristPoint.position;
            }

            var rb = interactorGO.GetComponent<Rigidbody>();
            if (rb != null) return rb.position;

            return interactorGO.transform.position;
        }
    }
}
