using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using SafeDriver.Missions;

namespace SafeDriver.UI
{
    /// <summary>
    /// Panel de seleccion de niveles del menu principal. Genera un boton por cada
    /// LevelDefinition de la lista (en orden de desbloqueo) dentro del scroll.
    /// El primer nivel esta siempre disponible; los siguientes se desbloquean cuando
    /// LevelManager marca el anterior como completado (via LevelProgress).
    /// La jerarquia en escena la arma Editor/SetupLevelSelectPanel.cs.
    /// </summary>
    public class LevelSelectPanel : MonoBehaviour
    {
        [Header("Niveles (en orden de desbloqueo)")]
        [Tooltip("Lista de niveles del juego. Cada asset define nombre (displayName) y escena (sceneName). El orden de la lista es el orden de desbloqueo.")]
        [SerializeField] private LevelDefinition[] niveles;

        [Header("Referencias UI (las asigna el setup)")]
        [Tooltip("Content del ScrollRect donde se instancian los botones.")]
        [SerializeField] private RectTransform content;

        [Tooltip("Boton template (desactivado, hijo del content) que se clona por cada nivel.")]
        [SerializeField] private GameObject buttonTemplate;

        [Header("Estilo")]
        [Tooltip("Color del boton cuando el nivel esta bloqueado.")]
        [SerializeField] private Color lockedColor = new Color(0.886f, 0.863f, 0.808f);

        // Color original del template (= nivel desbloqueado); se captura en Awake.
        private Color unlockedColor = Color.white;
        private readonly List<GameObject> spawned = new List<GameObject>();

        void Awake()
        {
            if (buttonTemplate != null)
            {
                var img = buttonTemplate.GetComponent<Image>();
                if (img != null) unlockedColor = img.color;
            }
        }

        // Rebuild al abrir: refleja desbloqueos nuevos sin tener que salir del menu.
        void OnEnable() => Rebuild();

        /// <summary>Abre el panel (lo llama el boton Jugar).</summary>
        public void Show() => gameObject.SetActive(true);

        /// <summary>Cierra el panel (boton Volver).</summary>
        public void Hide() => gameObject.SetActive(false);

        /// <summary>
        /// Desbloquea todos los niveles de la lista y refresca el panel.
        /// Boton admin/testeo — permite probar cualquier nivel sin jugar la progresion.
        /// </summary>
        public void UnlockAll()
        {
            if (niveles == null) return;
            foreach (var def in niveles)
                if (def != null) LevelProgress.Unlock(def.levelId);
            Rebuild();
        }

        private void Rebuild()
        {
            if (content == null || buttonTemplate == null)
            {
                Debug.LogWarning("[LevelSelectPanel] Falta content o buttonTemplate. Correr SafeDriver/UI/Level Select en el editor.", this);
                return;
            }

            foreach (var go in spawned) Destroy(go);
            spawned.Clear();

            int count = niveles != null ? niveles.Length : 0;
            for (int i = 0; i < count; i++)
            {
                var def = niveles[i];
                if (def == null) continue;

                // El primer nivel de la lista esta siempre disponible.
                bool unlocked = i == 0 || LevelProgress.IsUnlocked(def.levelId);
                CreateButton(def, unlocked);
            }
        }

        private void CreateButton(LevelDefinition def, bool unlocked)
        {
            var go = Instantiate(buttonTemplate, content);
            go.name = "Level_" + def.levelId;
            go.SetActive(true);
            spawned.Add(go);

            var label = go.GetComponentInChildren<TextMeshProUGUI>(true);
            var img = go.GetComponent<Image>();
            var btn = go.GetComponent<Button>();

            if (unlocked)
            {
                int best = LevelProgress.GetBestScore(def.levelId);
                if (label != null)
                    label.text = best > 0
                        ? def.displayName + "\n<size=60%>Mejor puntaje: " + best + "</size>"
                        : def.displayName;
                if (img != null) img.color = unlockedColor;
                if (btn != null)
                {
                    btn.interactable = true;
                    btn.onClick.AddListener(() => LoadLevel(def));
                }
            }
            else
            {
                if (label != null)
                    label.text = def.displayName + "\n<size=60%>Bloqueado</size>";
                if (img != null) img.color = lockedColor;
                if (btn != null) btn.interactable = false;
            }
        }

        private static void LoadLevel(LevelDefinition def)
        {
            // Guard: si la escena no esta en Build Settings, LoadScene explota en build.
            if (!Application.CanStreamedLevelBeLoaded(def.sceneName))
            {
                Debug.LogError("[LevelSelectPanel] La escena '" + def.sceneName + "' del nivel '" +
                               def.displayName + "' no esta en Build Settings.");
                return;
            }
            LevelManager.LoadLevel(def);
        }
    }
}
