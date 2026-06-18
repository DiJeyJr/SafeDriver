using UnityEngine;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// "Compuerta" de la senda peatonal: sabe si hay algun vehiculo (el player o un NPC) dentro de la
    /// zona de peligro sobre el asfalto. El peaton la consulta ANTES de bajar del cordon: si no esta
    /// libre, espera. Asi el peaton no se manda a cruzar cuando vos (o un NPC) estan pasando.
    ///
    /// Funciona por DISTANCIA (no por trigger) para no depender de capas: los NPC viven en la capa
    /// fisica aislada "Traffic" y un trigger comun no los detectaria. Cachea player (por tag) y NPCs
    /// (TrafficVehicle) en Start.
    ///
    /// Colocar en el centro del cruce, alineado con el sentido de la calle (el eje Z local = a lo largo
    /// de la calle por donde vienen los autos).
    /// </summary>
    public class CrosswalkTrafficGate : MonoBehaviour
    {
        [Tooltip("Tag del auto del player.")]
        [SerializeField] private string playerTag = "PlayerVehicle";

        [Tooltip("Medio-tamaño de la zona de peligro (local). X = ancho de calle, Z = aproximacion a cada lado.")]
        [SerializeField] private Vector3 halfExtents = new Vector3(7f, 3f, 8f);

        private Transform player;
        private TrafficVehicle[] npcs;

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            player = p != null ? p.transform : null;
            npcs = Object.FindObjectsByType<TrafficVehicle>(FindObjectsSortMode.None);
        }

        /// <summary>True si NO hay ningun vehiculo dentro de la zona de peligro (seguro para cruzar).</summary>
        public bool IsClear
        {
            get
            {
                if (player != null && InZone(player.position)) return false;
                if (npcs != null)
                    foreach (var n in npcs)
                        if (n != null && InZone(n.transform.position)) return false;
                return true;
            }
        }

        private bool InZone(Vector3 worldPos)
        {
            Vector3 local = transform.InverseTransformPoint(worldPos);
            return Mathf.Abs(local.x) <= halfExtents.x
                && Mathf.Abs(local.z) <= halfExtents.z;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);
        }
    }
}
