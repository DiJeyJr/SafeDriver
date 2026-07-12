using TMPro;
using UnityEngine;

namespace SafeDriver.UI
{
    /// <summary>
    /// Indicador de FPS siempre visible. Se auto-instancia al arrancar el juego
    /// (no requiere wiring en ninguna escena), sobrevive los cambios de escena y
    /// flota en la esquina superior derecha de la vista siguiendo a la camara.
    /// Promedia los frames de cada intervalo y colorea segun el target de Quest (72 Hz).
    /// </summary>
    public class FpsCounter : MonoBehaviour
    {
        // Offset local respecto a la camara (esquina superior derecha de la vista).
        private static readonly Vector3 Offset = new Vector3(0.13f, 0.12f, 0.38f);
        private const float UpdateInterval = 0.5f;
        private const string PrefKey = "sd_fps_visible";

        private static FpsCounter instance;

        /// <summary>Visibilidad del contador (persistida). Por defecto encendido.</summary>
        public static bool Visible
        {
            get => PlayerPrefs.GetInt(PrefKey, 1) == 1;
            set
            {
                PlayerPrefs.SetInt(PrefKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (instance != null) instance.ApplyVisibility();
            }
        }

        private static readonly Color ColorOk = new Color(0.29f, 0.76f, 0.38f);   // verde: cerca de 72
        private static readonly Color ColorWarn = new Color(0.98f, 0.73f, 0.25f); // amarillo: bajon notable
        private static readonly Color ColorBad = new Color(0.945f, 0.38f, 0.38f); // rojo: injugable

        private TextMeshPro label;
        private Camera cam;
        private int frames;
        private float elapsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<FpsCounter>() != null) return;
            var go = new GameObject("FpsCounter");
            DontDestroyOnLoad(go);
            go.AddComponent<FpsCounter>();
        }

        void Awake()
        {
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(transform, false);
            textGo.transform.localScale = Vector3.one * 0.01f;

            label = textGo.AddComponent<TextMeshPro>();
            label.fontSize = 20f;
            label.alignment = TextAlignmentOptions.Center;
            label.text = "-- FPS";
            label.color = ColorOk;
            // Dibujar por encima de otros transparentes (el texto igual respeta el depth de opacos).
            var mr = textGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = 32767;

            instance = this;
            ApplyVisibility();
        }

        void OnDestroy()
        {
            if (instance == this) instance = null;
        }

        private void ApplyVisibility() => label.gameObject.SetActive(Visible);

        void LateUpdate()
        {
            // No nos parenteamos a la camara (la destruye el cambio de escena): la seguimos a mano.
            if (cam == null || !cam.isActiveAndEnabled) cam = Camera.main;
            if (cam != null)
            {
                transform.position = cam.transform.TransformPoint(Offset);
                transform.rotation = cam.transform.rotation;
            }

            // Con el contador oculto no hay nada que actualizar.
            if (!label.gameObject.activeSelf) return;

            // unscaled: el menu de pausa pone timeScale = 0 y el contador debe seguir vivo.
            frames++;
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < UpdateInterval) return;

            float fps = frames / elapsed;
            frames = 0;
            elapsed = 0f;

            label.text = Mathf.RoundToInt(fps) + " FPS";
            label.color = fps >= 66f ? ColorOk : fps >= 45f ? ColorWarn : ColorBad;
        }
    }
}
