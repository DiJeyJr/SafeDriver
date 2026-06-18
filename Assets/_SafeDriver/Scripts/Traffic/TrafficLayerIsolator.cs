using UnityEngine;

namespace SafeDriver.Traffic
{
    /// <summary>
    /// Aisla la capa fisica "Traffic": en Awake hace que SOLO colisione consigo misma (ignora la
    /// colision con todas las demas capas). Asi el hitbox solido del auto del player choca con los
    /// autos NPC (ambos en "Traffic") pero NO se engancha con la calle, edificios ni nada en Default.
    ///
    /// Se hace por codigo (Physics.IgnoreLayerCollision) para no depender de editar la matriz de
    /// colisiones del proyecto. Idempotente: correr en Awake cada vez es seguro.
    /// </summary>
    public class TrafficLayerIsolator : MonoBehaviour
    {
        [SerializeField] private string layerName = "Traffic";

        void Awake()
        {
            int traffic = LayerMask.NameToLayer(layerName);
            if (traffic < 0)
            {
                Debug.LogWarning("[TrafficLayerIsolator] No existe la capa '" + layerName + "'. Los autos NPC no seran solidos.");
                return;
            }
            for (int i = 0; i < 32; i++)
                if (i != traffic) Physics.IgnoreLayerCollision(traffic, i, true);
        }
    }
}
