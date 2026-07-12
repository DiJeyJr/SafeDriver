using System.Collections;
using UnityEngine;

namespace SafeDriver.VR
{
    /// <summary>
    /// Pide al OS de Quest la frecuencia de refresco elegida por el usuario (persistida,
    /// default 90 Hz en vez de los 72 default del OS). Si el visor no soporta el valor
    /// exacto, usa la mas alta disponible que no lo supere. Se auto-instancia al arrancar;
    /// el slider de opciones del menu la cambia en vivo via Target.
    /// </summary>
    public class DisplayRefreshRate : MonoBehaviour
    {
        private const string PrefKey = "sd_refresh_hz";

        private static DisplayRefreshRate instance;
        private bool ready;

        /// <summary>Frecuencia objetivo en Hz (persistida). Al setearla se aplica al toque.</summary>
        public static float Target
        {
            get => PlayerPrefs.GetFloat(PrefKey, 90f);
            set
            {
                PlayerPrefs.SetFloat(PrefKey, value);
                PlayerPrefs.Save();
                if (instance != null && instance.ready) instance.Apply();
            }
        }

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
            instance = this;

            // Esperar a que OVRManager/OVRPlugin esten inicializados.
            while (OVRManager.instance == null || !OVRManager.OVRManagerinitialized)
                yield return null;

            ready = true;
            Apply();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void Apply()
        {
            float[] disponibles = OVRManager.display != null
                ? OVRManager.display.displayFrequenciesAvailable
                : null;
            if (disponibles == null || disponibles.Length == 0) return;

            // La mas alta disponible que no supere el target elegido.
            float target = Target;
            float mejor = 0f;
            foreach (float hz in disponibles)
                if (hz <= target + 0.5f && hz > mejor) mejor = hz;

            if (mejor > 0f)
            {
                OVRManager.display.displayFrequency = mejor;
                Debug.Log("[DisplayRefreshRate] Frecuencia pedida: " + mejor + " Hz (target " + target +
                          ", disponibles: " + string.Join(", ", disponibles) + ")");
            }
        }
    }
}
