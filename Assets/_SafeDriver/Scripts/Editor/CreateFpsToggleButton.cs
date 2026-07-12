using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Crea el boton "Ocultar FPS" en el MainMenuCanvas, debajo de Salir.
    /// El wiring lo hace el propio FpsToggleButton en runtime (no requiere persistent calls).
    /// Re-entrante: si ya existe lo regenera.
    /// </summary>
    public static class CreateFpsToggleButton
    {
        private const string ButtonName = "FpsToggleButton";

        [MenuItem("SafeDriver/UI/Agregar boton FPS (escena MainMenu)")]
        public static void Run()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                Debug.LogError("[FpsToggle] Abrir la escena MainMenu antes de correr esto.");
                return;
            }

            var t = UIComposer.LoadTheme();
            if (t == null) { Debug.LogError("[FpsToggle] No se encontro EduTheme.asset."); return; }

            var canvasGo = GameObject.Find("MainMenuCanvas");
            if (canvasGo == null) { Debug.LogError("[FpsToggle] No se encontro MainMenuCanvas."); return; }

            var previo = canvasGo.transform.Find(ButtonName);
            if (previo != null) Object.DestroyImmediate(previo.gameObject);

            // Debajo de QuitButton (que esta en anchoredPosition (0,-110), 420x110, anclado al centro).
            var btn = UIComposer.RoundButton(canvasGo.transform, ButtonName, "Ocultar FPS", t,
                UIThemeUtil.Role.Brand, Vector2.zero, new Vector2(280f, 70f));
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -205f);

            btn.gameObject.AddComponent<FpsToggleButton>();

            // Que quede antes que el LevelSelectPanel para no taparlo cuando el panel abra.
            var panel = canvasGo.transform.Find("LevelSelectPanel");
            if (panel != null) btn.transform.SetSiblingIndex(panel.GetSiblingIndex());

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[FpsToggle] Boton FPS creado en el menu. Guardar la escena.");
        }
    }
}
