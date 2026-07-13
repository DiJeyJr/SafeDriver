using UnityEngine;

namespace SafeDriver.Guidance
{
    /// <summary>
    /// Checkpoint individual de una CheckpointRoute. No se configura a mano: la ruta
    /// lo agrega y wirea sola a cada hijo en Start. Solo detecta el paso del player
    /// y le avisa a la ruta.
    /// </summary>
    public class Checkpoint : MonoBehaviour
    {
        [HideInInspector] public CheckpointRoute route;
        [HideInInspector] public int index;

        void OnTriggerEnter(Collider other)
        {
            if (route != null && other.CompareTag(route.PlayerTag))
                route.NotifyReached(index);
        }
    }
}
