using UnityEngine;

namespace SafeDriver.Guidance
{
    /// <summary>Gira el GameObject para que siempre mire a la camara del jugador (carteles).</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam != null)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
        }
    }
}
