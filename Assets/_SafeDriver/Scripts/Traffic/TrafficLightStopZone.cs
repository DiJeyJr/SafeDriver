using UnityEngine;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Zona de detencion para autos NPC. Al colocarse antes de un semaforo en el carril
    /// correspondiente, los <see cref="TrafficVehicle"/> que la atraviesan consultan
    /// <see cref="ShouldStop"/> para frenar mientras la luz este en rojo o amarillo.
    ///
    /// Requiere un Collider con `isTrigger=true` en el mismo GameObject. La logica de
    /// deteccion la hace <see cref="TrafficVehicle"/> en sus OnTriggerEnter/Exit.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrafficLightStopZone : MonoBehaviour
    {
        [Tooltip("TrafficLightController cuya luz dicta si los NPCs deben frenar.")]
        [SerializeField] private TrafficLightController trafficLight;

        [Tooltip("Si esta activo, los NPCs tambien frenan en amarillo (no solo rojo).")]
        [SerializeField] private bool stopOnYellow = true;

        /// <summary>True si los NPCs deben frenar al estar dentro de esta zona.</summary>
        public bool ShouldStop
        {
            get
            {
                if (trafficLight == null) return false;
                if (trafficLight.IsRed()) return true;
                if (stopOnYellow && trafficLight.IsYellow()) return true;
                return false;
            }
        }
    }
}
