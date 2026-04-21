using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;

namespace SafeDriver.VR
{
    /// <summary>
    /// Limita el grab del volante a interactors cuya posicion este dentro
    /// del SphereCollider del volante.
    ///
    /// Necesario porque GrabInteractable auto-coleciona TODOS los colliders del
    /// Rigidbody asociado (chasis del auto), por lo que sin este filtro el volante
    /// se puede agarrar desde cualquier collider del auto. Asignar este componente
    /// al campo _interactorFilters de GrabInteractable.
    ///
    /// En el rig BB "Controller and Hand", los interactors tienen su transform fijo en
    /// TrackingSpace; solo WristPoint/PalmPoint y el Rigidbody (via HandGrabRigidbodyTracker)
    /// se actualizan con el tracking. Por eso probamos en orden: WristPoint > Rigidbody > transform.
    /// </summary>
    public class WheelGrabProximityFilter : MonoBehaviour, IGameObjectFilter
    {
        [Tooltip("Hitbox del volante. Si queda vacio se busca un SphereCollider en este GameObject.")]
        [SerializeField] private SphereCollider hitbox;

        [Tooltip("Multiplicador del radio para tolerancia. 1.0 = radio exacto.")]
        [SerializeField] private float radiusMultiplier = 1f;

        [SerializeField] private bool logDebug = false;
        private float lastLog;

        void Awake()
        {
            if (hitbox == null) hitbox = GetComponent<SphereCollider>();
        }

        public bool Filter(GameObject interactorGameObject)
        {
            if (hitbox == null) return true;

            Vector3 worldCenter = hitbox.transform.TransformPoint(hitbox.center);
            float worldRadius = hitbox.radius
                * Mathf.Max(hitbox.transform.lossyScale.x,
                            hitbox.transform.lossyScale.y,
                            hitbox.transform.lossyScale.z)
                * radiusMultiplier;

            Vector3 probe = ResolveTrackedPosition(interactorGameObject);
            float dist = Vector3.Distance(probe, worldCenter);
            bool inside = dist <= worldRadius;

            if (logDebug && Time.time - lastLog > 0.5f)
            {
                lastLog = Time.time;
                Debug.Log($"[WheelFilter] interactor={interactorGameObject.name} probe={probe} wheelCenter={worldCenter} radius={worldRadius:F3} dist={dist:F3} inside={inside}");
            }
            return inside;
        }

        private static Vector3 ResolveTrackedPosition(GameObject interactorGO)
        {
            var hgi = interactorGO.GetComponent<HandGrabInteractor>();
            if (hgi != null && hgi.WristPoint != null) return hgi.WristPoint.position;

            var rb = interactorGO.GetComponent<Rigidbody>();
            if (rb != null) return rb.position;

            return interactorGO.transform.position;
        }
    }
}
