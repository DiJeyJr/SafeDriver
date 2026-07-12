using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Arma la fila de opciones de FPS del MainMenuCanvas: boton "Ocultar FPS" (izquierda)
    /// + slider de limite de refresco 72/80/90/120 Hz (derecha). El wiring lo hacen los
    /// propios componentes en runtime (sin persistent calls). Re-entrante.
    /// </summary>
    public static class CreateFpsToggleButton
    {
        private const string ButtonName = "FpsToggleButton";
        private const string SliderName = "RefreshSlider";

        [MenuItem("SafeDriver/UI/Agregar opciones FPS (escena MainMenu)")]
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

            var prevBtn = canvasGo.transform.Find(ButtonName);
            if (prevBtn != null) Object.DestroyImmediate(prevBtn.gameObject);
            var prevSlider = canvasGo.transform.Find(SliderName);
            if (prevSlider != null) Object.DestroyImmediate(prevSlider.gameObject);

            // ---- Boton toggle (abajo a la izquierda) ----
            var btn = UIComposer.RoundButton(canvasGo.transform, ButtonName, "Ocultar FPS", t,
                UIThemeUtil.Role.Brand, Vector2.zero, new Vector2(260f, 70f));
            var btnRt = btn.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(-165f, -205f);
            btn.gameObject.AddComponent<FpsToggleButton>();

            // ---- Slider de refresco (abajo a la derecha) ----
            var sliderGo = new GameObject(SliderName, typeof(RectTransform));
            sliderGo.transform.SetParent(canvasGo.transform, false);
            var sRt = sliderGo.GetComponent<RectTransform>();
            sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 0.5f);
            sRt.pivot = new Vector2(0.5f, 0.5f);
            sRt.anchoredPosition = new Vector2(165f, -215f);
            sRt.sizeDelta = new Vector2(260f, 36f);

            // Background de la barra
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer));
            bgGo.transform.SetParent(sliderGo.transform, false);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.5f); bgRt.anchorMax = new Vector2(1f, 0.5f);
            bgRt.sizeDelta = new Vector2(0f, 14f);
            var bgImg = bgGo.AddComponent<Image>();
            Slice(bgImg, t); bgImg.color = t.border;

            // Fill
            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            var faRt = fillAreaGo.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.5f); faRt.anchorMax = new Vector2(1f, 0.5f);
            faRt.offsetMin = new Vector2(7f, -7f); faRt.offsetMax = new Vector2(-7f, 7f);
            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.sizeDelta = new Vector2(10f, 0f);
            var fillImg = fillGo.AddComponent<Image>();
            Slice(fillImg, t); fillImg.color = t.brand;

            // Handle
            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            var haRt = handleAreaGo.GetComponent<RectTransform>();
            haRt.anchorMin = new Vector2(0f, 0.5f); haRt.anchorMax = new Vector2(1f, 0.5f);
            haRt.offsetMin = new Vector2(14f, 0f); haRt.offsetMax = new Vector2(-14f, 0f);
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var hRt = handleGo.GetComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(32f, 32f);
            var hImg = handleGo.AddComponent<Image>();
            Slice(hImg, t); hImg.color = Color.white;

            var slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = hImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = 0;
            slider.maxValue = 3;
            slider.value = 2; // 90 Hz (el default; el componente lo re-lee de la pref en runtime)

            // Label del valor (arriba de la barra)
            var label = UIComposer.Text(sliderGo.transform, "90 Hz", t, UIThemeUtil.TextKind.Body,
                new Vector2(0f, 1f), new Vector2(1f, 1f));
            var lRt = label.GetComponent<RectTransform>();
            lRt.pivot = new Vector2(0.5f, 0f);
            lRt.anchoredPosition = new Vector2(0f, 4f);
            lRt.sizeDelta = new Vector2(0f, 28f);

            var comp = sliderGo.AddComponent<RefreshRateSlider>();
            var so = new SerializedObject(comp);
            so.FindProperty("valueLabel").objectReferenceValue = label;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Que queden antes del LevelSelectPanel para que el panel los tape al abrirse.
            var panel = canvasGo.transform.Find("LevelSelectPanel");
            if (panel != null)
            {
                btn.transform.SetSiblingIndex(panel.GetSiblingIndex());
                sliderGo.transform.SetSiblingIndex(panel.GetSiblingIndex());
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[FpsToggle] Opciones FPS creadas: toggle + slider de refresco. Guardar la escena.");
        }

        private static void Slice(Image img, SafeDriver.UI.UITheme t)
        {
            if (t.roundedSprite != null) { img.sprite = t.roundedSprite; img.type = Image.Type.Sliced; }
        }
    }
}
