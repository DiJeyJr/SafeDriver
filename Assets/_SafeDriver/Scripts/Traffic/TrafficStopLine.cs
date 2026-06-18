using UnityEngine;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Linea de detencion para autos NPC frente a un semaforo. Es un PUNTO (no un trigger): los
    /// TrafficVehicle consultan todas las lineas y, si una esta en rojo, en su carril y ADELANTE
    /// (todavia no la pasaron), desaceleran para parar justo antes de ella. Una vez que la pasan, la
    /// ignoran → terminan de cruzar y NO se quedan parados en medio del cruce.
    ///
    /// Mover la POSICION de este objeto ajusta donde frenan los NPC (ponerla en la linea de detencion,
    /// antes de la senda). Funciona por distancia, no depende de capas ni triggers.
    /// </summary>
    public class TrafficStopLine : MonoBehaviour
    {
        [SerializeField] private TrafficLightController trafficLight;
        [SerializeField] private bool stopOnYellow = true;

        public Vector3 Position => transform.position;
        public bool IsStop => trafficLight != null
            && (trafficLight.IsRed() || (stopOnYellow && trafficLight.IsYellow()));

        public void SetLight(TrafficLightController light) { trafficLight = light; }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position + transform.right * 3f, transform.position - transform.right * 3f);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2f);
        }
    }
}
