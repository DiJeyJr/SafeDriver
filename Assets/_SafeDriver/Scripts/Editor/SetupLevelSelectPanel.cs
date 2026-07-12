using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SafeDriver.Missions;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Arma el panel de seleccion de niveles en la escena MainMenu (overlay con scroll,
    /// estilo EduTheme) y wirea el boton Jugar para abrirlo. Re-entrante: si el panel ya
    /// existe lo regenera preservando la lista de niveles configurada.
    ///
    /// Item 2: sincroniza la cadena de desbloqueo (nextLevel) de los LevelDefinition
    /// segun el orden de la lista del panel, y agrega a Build Settings las escenas
    /// que falten. Correrlo cada vez que se cambie la lista.
    /// </summary>
    public static class SetupLevelSelectPanel
    {
        private const string PanelName = "LevelSelectPanel";
        private const string Nivel1AssetPath = "Assets/_SafeDriver/Missions/Levels/Level_01_City.asset";

        [MenuItem("SafeDriver/UI/Level Select/1. Armar panel de niveles (escena MainMenu)")]
        public static void BuildPanel()
        {
            if (SceneManager.GetActiveScene().name != "MainMenu")
            {
                Debug.LogError("[LevelSelect] Abrir la escena MainMenu antes de correr este setup.");
                return;
            }

            var t = UIComposer.LoadTheme();
            if (t == null) { Debug.LogError("[LevelSelect] No se encontro EduTheme.asset."); return; }

            var canvasGo = GameObject.Find("MainMenuCanvas");
            if (canvasGo == null) { Debug.LogError("[LevelSelect] No se encontro MainMenuCanvas."); return; }

            // Si ya existe, preservar la lista de niveles configurada y regenerar.
            var niveles = new List<LevelDefinition>();
            var previo = canvasGo.transform.Find(PanelName);
            if (previo != null)
            {
                var prevPanel = previo.GetComponent<LevelSelectPanel>();
                if (prevPanel != null)
                {
                    var soPrev = new SerializedObject(prevPanel);
                    var arr = soPrev.FindProperty("niveles");
                    for (int i = 0; i < arr.arraySize; i++)
                    {
                        var def = arr.GetArrayElementAtIndex(i).objectReferenceValue as LevelDefinition;
                        if (def != null) niveles.Add(def);
                    }
                }
                Object.DestroyImmediate(previo.gameObject);
            }
            if (niveles.Count == 0)
            {
                var nivel1 = AssetDatabase.LoadAssetAtPath<LevelDefinition>(Nivel1AssetPath);
                if (nivel1 != null) niveles.Add(nivel1);
                else Debug.LogWarning("[LevelSelect] No se encontro " + Nivel1AssetPath + " — la lista queda vacia.");
            }

            // ---- Panel raiz: overlay full-canvas con fondo dim ----
            var panelGo = new GameObject(PanelName, typeof(RectTransform), typeof(CanvasRenderer));
            panelGo.transform.SetParent(canvasGo.transform, false);
            panelGo.transform.SetAsLastSibling(); // dibuja arriba de los botones del menu
            var panelRt = panelGo.GetComponent<RectTransform>();
            panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
            panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;
            var dim = panelGo.AddComponent<Image>();
            UIComposer.DimBackground(dim, 0.45f); // ademas bloquea el raycast a los botones de atras

            // ---- Card central ----
            var card = UIComposer.Card(panelGo.transform, t, new Vector2(560f, 440f));

            var header = UIComposer.HeaderBand(card, t, t.brand, 64f);
            var title = UIComposer.Text(header, "NIVELES", t, UIThemeUtil.TextKind.Heading,
                Vector2.zero, Vector2.one);
            title.color = t.textOnAccent;

            // ---- Area de scroll (entre header y boton Volver) ----
            var scrollGo = new GameObject("ScrollArea", typeof(RectTransform));
            scrollGo.transform.SetParent(card, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(16f, 96f);   // abajo: lugar para Volver
            scrollRt.offsetMax = new Vector2(-16f, -72f); // arriba: header 64 + margen
            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            // Viewport: la Image casi invisible es necesaria para el mask/raycast del drag.
            var viewportGo = new GameObject("Viewport", typeof(RectTransform));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
            viewportRt.pivot = new Vector2(0.5f, 1f);
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = new Vector2(-20f, 0f); // lugar para el scrollbar (12) + margen
            var vpImg = viewportGo.AddComponent<Image>();
            vpImg.color = new Color(1f, 1f, 1f, 0.001f);
            viewportGo.AddComponent<RectMask2D>();

            // Content: acá el panel instancia los botones en runtime.
            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f); contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Scrollbar vertical (replica del panel de objetivos del tablero).
            var sbGo = new GameObject("ScrollbarV", typeof(RectTransform), typeof(CanvasRenderer));
            sbGo.transform.SetParent(scrollGo.transform, false);
            var sbRt = sbGo.GetComponent<RectTransform>();
            sbRt.anchorMin = new Vector2(1f, 0f); sbRt.anchorMax = Vector2.one;
            sbRt.pivot = new Vector2(1f, 0.5f);
            sbRt.anchoredPosition = new Vector2(-4f, 0f);
            sbRt.sizeDelta = new Vector2(12f, -16f);
            var sbImg = sbGo.AddComponent<Image>();
            if (t.roundedSprite != null) { sbImg.sprite = t.roundedSprite; sbImg.type = Image.Type.Sliced; }
            sbImg.color = new Color(t.border.r, t.border.g, t.border.b, 0.6f);
            var sb = sbGo.AddComponent<Scrollbar>();
            sb.direction = Scrollbar.Direction.BottomToTop;

            var slideGo = new GameObject("Sliding Area", typeof(RectTransform));
            slideGo.transform.SetParent(sbGo.transform, false);
            var slideRt = slideGo.GetComponent<RectTransform>();
            slideRt.anchorMin = Vector2.zero; slideRt.anchorMax = Vector2.one;
            slideRt.offsetMin = new Vector2(1f, 1f); slideRt.offsetMax = new Vector2(-1f, -1f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer));
            handleGo.transform.SetParent(slideGo.transform, false);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.offsetMin = Vector2.zero; handleRt.offsetMax = Vector2.zero;
            var handleImg = handleGo.AddComponent<Image>();
            if (t.roundedSprite != null) { handleImg.sprite = t.roundedSprite; handleImg.type = Image.Type.Sliced; }
            handleImg.color = t.brand;
            sb.handleRect = handleRt;
            sb.targetGraphic = handleImg;

            scroll.viewport = viewportRt;
            scroll.content = contentRt;
            scroll.verticalScrollbar = sb;
            // AutoHide (NO AutoHideAndExpandViewport: redimensiona el viewport y rompe el layout).
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // ---- Template de boton de nivel (se clona en runtime) ----
            var templateBtn = UIComposer.RoundButton(contentGo.transform, "LevelButtonTemplate", "Nivel",
                t, UIThemeUtil.Role.Brand, Vector2.zero, new Vector2(0f, 96f));
            templateBtn.gameObject.SetActive(false);

            // ---- Boton Volver ----
            var volverBtn = UIComposer.RoundButton(card, "VolverButton", "Volver", t,
                UIThemeUtil.Role.Warning, new Vector2(0f, 16f), new Vector2(240f, 64f));

            // ---- Componente del panel + wiring ----
            var panel = panelGo.AddComponent<LevelSelectPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("content").objectReferenceValue = contentRt;
            so.FindProperty("buttonTemplate").objectReferenceValue = templateBtn.gameObject;
            var arrProp = so.FindProperty("niveles");
            arrProp.arraySize = niveles.Count;
            for (int i = 0; i < niveles.Count; i++)
                arrProp.GetArrayElementAtIndex(i).objectReferenceValue = niveles[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            // Volver -> Hide (persistent call, mismo patron que MainMenuWiring).
            for (int i = volverBtn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(volverBtn.onClick, i);
            UnityEventTools.AddPersistentListener(volverBtn.onClick, new UnityAction(panel.Hide));

            // Jugar abre el panel: asignar la referencia en MainMenuController.
            var menuCtrl = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
            if (menuCtrl != null)
            {
                var soCtrl = new SerializedObject(menuCtrl);
                soCtrl.FindProperty("levelSelectPanel").objectReferenceValue = panel;
                soCtrl.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[LevelSelect] No se encontro MainMenuController para wirear Jugar.");

            // Arranca oculto; lo abre el boton Jugar.
            panelGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[LevelSelect] Panel armado con " + niveles.Count + " nivel(es). Guardar la escena.");
        }

        [MenuItem("SafeDriver/UI/Level Select/2. Sync cadena de desbloqueo + Build Settings")]
        public static void SyncChain()
        {
            var panel = Object.FindFirstObjectByType<LevelSelectPanel>(FindObjectsInactive.Include);
            if (panel == null) { Debug.LogError("[LevelSelect] No hay LevelSelectPanel en la escena abierta."); return; }

            var so = new SerializedObject(panel);
            var arr = so.FindProperty("niveles");
            var niveles = new List<LevelDefinition>();
            for (int i = 0; i < arr.arraySize; i++)
            {
                var def = arr.GetArrayElementAtIndex(i).objectReferenceValue as LevelDefinition;
                if (def != null) niveles.Add(def);
            }
            if (niveles.Count == 0) { Debug.LogWarning("[LevelSelect] Lista de niveles vacia, nada que sincronizar."); return; }

            // nextLevel de cada asset = el siguiente de la lista (el ultimo queda sin siguiente).
            for (int i = 0; i < niveles.Count; i++)
            {
                var next = i + 1 < niveles.Count ? niveles[i + 1] : null;
                if (niveles[i].nextLevel != next)
                {
                    niveles[i].nextLevel = next;
                    EditorUtility.SetDirty(niveles[i]);
                }
            }

            // Agregar a Build Settings las escenas de la lista que falten.
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int agregadas = 0;
            foreach (var def in niveles)
            {
                if (string.IsNullOrEmpty(def.sceneName)) continue;
                string path = FindScenePath(def.sceneName);
                if (path == null)
                {
                    Debug.LogWarning("[LevelSelect] Escena '" + def.sceneName + "' (" + def.displayName + ") no existe en el proyecto.");
                    continue;
                }
                if (buildScenes.Exists(s => s.path == path)) continue;
                buildScenes.Add(new EditorBuildSettingsScene(path, true));
                agregadas++;
            }
            if (agregadas > 0) EditorBuildSettings.scenes = buildScenes.ToArray();

            AssetDatabase.SaveAssets();
            EditorApplication.ExecuteMenuItem("File/Save Project"); // persiste Build Settings a disco
            Debug.Log("[LevelSelect] Cadena sincronizada (" + niveles.Count + " niveles). Escenas agregadas al build: " + agregadas + ".");
        }

        /// <summary>Abre el panel a mano (util para probar en play mode sin visor).</summary>
        [MenuItem("SafeDriver/UI/Level Select/Debug - Abrir panel")]
        public static void DebugOpenPanel()
        {
            var panel = Object.FindFirstObjectByType<LevelSelectPanel>(FindObjectsInactive.Include);
            if (panel == null) { Debug.LogError("[LevelSelect] No hay LevelSelectPanel en la escena."); return; }
            panel.Show();
            Debug.Log("[LevelSelect] Panel abierto (debug).");
        }

        private static string FindScenePath(string sceneName)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:SceneAsset " + sceneName))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName) return path;
            }
            return null;
        }
    }
}
