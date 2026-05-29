using UnityEngine;
using SafeDriver.Core;

namespace SafeDriver.Scoring
{
    /// <summary>
    /// Zona que detecta si el auto circula en contramano. Define una direccion
    /// permitida (allowedLocalDirection, por defecto el eje Z local del objeto); al
    /// entrar el player, compara su rumbo con esa direccion via producto punto. Si va
    /// en sentido opuesto (dot < -threshold), dispara WrongWay.
    ///
    /// Solo penaliza (no premia). Un disparo por pasada para no spamear.
    /// Colocar con un BoxCollider trigger cubriendo el carril, orientado de modo que
    /// el eje Z local apunte en el sentido legal de circulacion.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DirectionZone : VehicleSensor
    {
        [Header("Direccion permitida")]
        [Tooltip("Direccion local de circulacion legal. Default: eje Z local (forward).")]
        [SerializeField] private Vector3 allowedLocalDirection = Vector3.forward;

        [Tooltip("Umbral de oposicion: dot por debajo de -este valor cuenta como contramano. 0.5 ~ mas de 120 grados opuesto.")]
        [Range(0f, 1f)]
        [SerializeField] private float oppositeThreshold = 0.5f;

        [Tooltip("Velocidad minima (km/h) para evaluar el rumbo por velocidad. Por debajo usa el forward del auto.")]
        [SerializeField] private float minSpeedForHeading = 3f;

        private bool firedThisPass;

        void Awake()
        {
            infractionType = InfractionType.WrongWay;
            pedagogicalMessage =
                "Vas en contramano. Circula siempre en el sentido permitido del carril.";
        }

        private Vector3 AllowedDirectionWorld => transform.TransformDirection(allowedLocalDirection.normalized);

        void OnTriggerEnter(Collider other)
        {
            if (firedThisPass || !IsPlayer(other)) return;
            if (Vehicle == null) return;

            // Rumbo: por velocidad si se mueve, si no por el forward del auto.
            Vector3 heading;
            var rb = other.attachedRigidbody;
            if (rb != null && Vehicle.CurrentSpeedKmh >= minSpeedForHeading)
                heading = rb.linearVelocity.normalized;
            else
                heading = Vehicle.transform.forward;

            float dot = Vector3.Dot(heading, AllowedDirectionWorld);
            if (dot < -oppositeThreshold)
            {
                firedThisPass = true;
                infractionType = InfractionType.WrongWay;
                TriggerInfraction();
            }
        }

        void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other)) firedThisPass = false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Vector3 dir = AllowedDirectionWorld;
            Gizmos.DrawLine(transform.position, transform.position + dir * 3f);
            // Punta de flecha
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized;
            Gizmos.DrawLine(transform.position + dir * 3f, transform.position + dir * 2.4f + right * 0.4f);
            Gizmos.DrawLine(transform.position + dir * 3f, transform.position + dir * 2.4f - right * 0.4f);
        }
    }
}
