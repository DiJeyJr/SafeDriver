using System.Collections;
using UnityEngine;

namespace SafeDriver.VR
{
    /// <summary>
    /// Pide al OS de Quest una frecuencia de refresco mayor a los 72 Hz default.
    /// Intenta la frecuencia objetivo (90) y si el visor no la soporta se queda con
    /// la mas alta disponible que no la supere. Se auto-instancia en el arranque.
    /// </summary>
    public class DisplayRefreshRate : MonoBehaviour
    {
        // 90 es el sweet spot en Quest 3: mas fluido que 72 y alcanzable en performance.
        // (120 existe pero si el juego no lo sostiene, la reproyeccion se siente peor.)
        private const float TargetHz = 90f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<DisplayRefreshRate>() != null) return;
            var go = new GameObject("DisplayRefreshRate");
            DontDestroyOnLoad(go);
            go.AddComponent<DisplayRefreshRate>();
        }

        IEnumerator Start()
        {
            // Esperar a que OVRManager/OVRPlugin esten inicializados.
            while (OVRManager.instance == null || !OVRManager.OVRManagerinitialized)
                yield return null;

            float[] disponibles = OVRManager.display != null
                ? OVRManager.display.displayFrequenciesAvailable
                : null;
            if (disponibles == null || disponibles.Length == 0) yield break;

            // La mas alta que no supere el target.
            float mejor = 0f;
            foreach (float hz in disponibles)
                if (hz <= TargetHz + 0.5f && hz > mejor) mejor = hz;

            if (mejor > 0f)
            {
                OVRManager.display.displayFrequency = mejor;
                Debug.Log("[DisplayRefreshRate] Frecuencia pedida: " + mejor + " Hz (disponibles: " +
                          string.Join(", ", disponibles) + ")");
            }
        }
    }
}
