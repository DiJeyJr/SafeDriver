using UnityEngine;

namespace SafeDriver.VR
{
    /// <summary>
    /// Aplica un shake breve al target (tipicamente el CenterEyeAnchor) sumando un offset
    /// local aleatorio que decae linealmente. Singleton — se llama via `CameraShake.Instance.Shake(...)`.
    ///
    /// En VR el shake fuerte da nauseas; mantener magnitud chica (menos de 5 cm) y duracion corta.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Tooltip("Transform al que se le aplica el offset. Si queda vacio, usa este.")]
        [SerializeField] private Transform target;

        private Vector3 originalLocalPos;
        private float remaining;
        private float totalDuration;
        private float magnitude;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            if (target == null) target = transform;
            originalLocalPos = target.localPosition;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (target != null) target.localPosition = originalLocalPos;
        }

        /// <summary>Dispara un shake. Si ya hay uno corriendo, el nuevo lo reemplaza (no se acumula).</summary>
        public void Shake(float magnitude, float duration)
        {
            if (target == null) return;
            this.magnitude = Mathf.Max(0f, magnitude);
            this.totalDuration = Mathf.Max(0f, duration);
            this.remaining = this.totalDuration;
        }

        void LateUpdate()
        {
            if (target == null) return;
            if (remaining <= 0f)
            {
                target.localPosition = originalLocalPos;
                return;
            }

            remaining -= Time.unscaledDeltaTime;
            float t = totalDuration > 0f ? remaining / totalDuration : 0f;
            float currentMag = magnitude * Mathf.Clamp01(t);
            var offset = Random.insideUnitSphere * currentMag;
            target.localPosition = originalLocalPos + offset;

            if (remaining <= 0f) target.localPosition = originalLocalPos;
        }
    }
}
