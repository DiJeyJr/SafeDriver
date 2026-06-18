using System;
using UnityEngine;
using SafeDriver.Core;
using SafeDriver.Vehicle;
using SafeDriver.Traffic;

namespace SafeDriver.Scoring
{
    /// <summary>Zonas de integridad del auto (8): carroceria + 4 ruedas.</summary>
    public enum IntegrityZone { Front, Rear, LeftSide, RightSide, WheelFL, WheelFR, WheelRL, WheelRR }

    /// <summary>
    /// Integridad del vehiculo por zonas. Va en el auto del player (el que tiene el BoxCollider trigger).
    /// Cuando el player roza/choca un obstaculo (NPC u objeto solido alto), detecta la ZONA golpeada
    /// (por el punto de contacto en espacio local del auto) y la GRAVEDAD (por la velocidad), y baja la
    /// integridad de esa zona. Si el golpe es grave, o si la integridad total quedo critica, dispara un
    /// SafeFail (InfractionType.SevereCollision, grave).
    ///
    /// Es ADITIVO: no toca la fisica del auto (solo escucha OnTriggerEnter). La integridad es una
    /// puntuacion aparte que se muestra en el simbolo del tablero y al final del nivel. Se resetea sola
    /// al recargar la escena (Awake reinicia las zonas a 100).
    /// </summary>
    public class VehicleIntegrity : MonoBehaviour
    {
        public static VehicleIntegrity Instance { get; private set; }

        [Header("Daño")]
        [Tooltip("Daño aplicado por cada km/h de velocidad al momento del impacto.")]
        [SerializeField] private float damagePerKmh = 0.8f;
        [SerializeField] private float minDamage = 6f;
        [SerializeField] private float maxDamage = 60f;

        [Header("SafeFail")]
        [Tooltip("Daño en un solo golpe que cuenta como choque GRAVE → dispara SafeFail.")]
        [SerializeField] private float severeDamageThreshold = 35f;
        [Tooltip("Integridad total (0..1) por debajo de la cual el auto esta demasiado daniado → SafeFail.")]
        [SerializeField] private float criticalIntegrity = 0.35f;

        [Header("Anti-spam")]
        [Tooltip("Segundos minimos entre dos impactos contables (evita spamear daño en un mismo roce).")]
        [SerializeField] private float damageCooldown = 0.3f;

        [Header("Dimensiones del auto (medias, para clasificar la zona)")]
        [SerializeField] private float halfWidth = 0.8f;
        [SerializeField] private float halfLength = 1.8f;

        /// <summary>Se dispara cuando cambia la integridad de alguna zona (la UI se suscribe).</summary>
        public event Action OnChanged;

        private const int ZoneCount = 8;
        private readonly float[] zones = new float[ZoneCount]; // 0..100 por zona
        private float lastDamageTime = -10f;

        /// <summary>Integridad total normalizada 0..1 (promedio de las 8 zonas).</summary>
        public float Overall01 { get; private set; } = 1f;
        public int OverallPercent => Mathf.RoundToInt(Overall01 * 100f);
        public float GetZone01(IntegrityZone z) => zones[(int)z] / 100f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            for (int i = 0; i < ZoneCount; i++) zones[i] = 100f;
            Overall01 = 1f;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        // Trigger: SOLO obstaculos solidos altos (edificios, postes) que el auto atraviesa por ser un
        // collider trigger. Los autos NPC ahora son solidos en la capa "Traffic" y se detectan por
        // OnCollisionEnter (colision fisica real). Se excluye el piso plano y los triggers de mision.
        void OnTriggerEnter(Collider other)
        {
            if (other.transform.IsChildOf(transform)) return;
            if (other.isTrigger || other.bounds.max.y <= 0.5f) return;
            RegisterHit(other.ClosestPoint(transform.position));
        }

        // Colision FISICA solida: el hitbox solido del auto (capa Traffic) contra un auto NPC solido.
        // Da punto de contacto exacto para clasificar la zona golpeada.
        void OnCollisionEnter(Collision collision)
        {
            if (collision.collider != null && collision.collider.transform.IsChildOf(transform)) return;
            Vector3 contact = collision.contactCount > 0
                ? collision.GetContact(0).point
                : transform.TransformPoint(new Vector3(0f, 0.6f, halfLength)); // fallback: frente
            RegisterHit(contact);
        }

        private void RegisterHit(Vector3 contactWorld)
        {
            if (Time.time - lastDamageTime < damageCooldown) return;

            // Zona golpeada por el punto de contacto en espacio local del auto.
            Vector3 local = transform.InverseTransformPoint(contactWorld);
            IntegrityZone zone = ClassifyZone(local);

            // Gravedad por velocidad del auto al impactar.
            float speed = VehicleController.Instance != null ? Mathf.Abs(VehicleController.Instance.CurrentSpeedKmh) : 12f;
            float damage = Mathf.Clamp(speed * damagePerKmh, minDamage, maxDamage);

            ApplyDamage(zone, damage);
            lastDamageTime = Time.time;

            bool severe = damage >= severeDamageThreshold;
            bool critical = Overall01 <= criticalIntegrity;
            if (severe || critical)
            {
                string msg = severe
                    ? "Chocaste fuerte contra otro vehiculo u objeto. Mantene distancia y una velocidad segura para frenar a tiempo."
                    : "El auto acumulo demasiado daño. Cada choque compromete tu seguridad: conduci con mas cuidado.";
                EventBus.Dispatch_Infraction(InfractionType.SevereCollision, msg);
            }
        }

        private void ApplyDamage(IntegrityZone zone, float damage)
        {
            int i = (int)zone;
            zones[i] = Mathf.Max(0f, zones[i] - damage);
            RecalcOverall();
            OnChanged?.Invoke();
        }

        private void RecalcOverall()
        {
            float sum = 0f;
            for (int i = 0; i < ZoneCount; i++) sum += zones[i];
            Overall01 = sum / (ZoneCount * 100f);
        }

        // Clasifica el contacto local en una de las 8 zonas. Las esquinas (|x| y |z| altos) son ruedas;
        // los bordes frontal/trasero son Front/Rear; los laterales son Left/Right.
        private IntegrityZone ClassifyZone(Vector3 local)
        {
            float nx = Mathf.Clamp(local.x / Mathf.Max(0.01f, halfWidth),  -1f, 1f); // -1 izq .. +1 der
            float nz = Mathf.Clamp(local.z / Mathf.Max(0.01f, halfLength), -1f, 1f); // -1 atras .. +1 frente

            bool corner = Mathf.Abs(nx) > 0.55f && Mathf.Abs(nz) > 0.45f;
            if (corner)
            {
                if (nz > 0f) return nx > 0f ? IntegrityZone.WheelFR : IntegrityZone.WheelFL;
                return nx > 0f ? IntegrityZone.WheelRR : IntegrityZone.WheelRL;
            }
            if (Mathf.Abs(nz) >= Mathf.Abs(nx))
                return nz > 0f ? IntegrityZone.Front : IntegrityZone.Rear;
            return nx > 0f ? IntegrityZone.RightSide : IntegrityZone.LeftSide;
        }
    }
}
