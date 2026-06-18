using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;
using TMPro;
using SafeDriver.UI;
using SafeDriver.Scoring;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Mejora la UI diegetica del tablero con el estilo nuevo:
    ///   - Panel de objetivos: fondo crema redondeado (en vez del negro) + colores de
    ///     texto legibles sobre fondo claro.
    ///
    /// Disparar: SafeDriver/UI/9. Restyle panel de objetivos.
    /// </summary>
    public static class RestyleDashboard
    {
        [MenuItem("SafeDriver/UI/9. Restyle panel de objetivos")]
        public static void RestyleObjectives()
        {
            var theme = UIComposer.LoadTheme();
            if (theme == null) { Debug.LogError("[Dashboard] No se encontro EduTheme."); return; }

            var oc = Object.FindFirstObjectByType<ObjectivesController>(FindObjectsInactive.Include);
            if (oc == null) { Debug.LogWarning("[Dashboard] No se encontro ObjectivesController."); return; }

            // El fondo esta en el padre del 'List' (ObjectivesPanel) o en el mismo objeto.
            var panel = oc.GetComponentInParent<Canvas>();
            Image bg = null;
            if (panel != null) bg = panel.GetComponent<Image>();
            if (bg == null) bg = oc.GetComponentInParent<Image>();

            if (bg != null)
            {
                if (theme.roundedSprite != null) { bg.sprite = theme.roundedSprite; bg.type = Image.Type.Sliced; }
                bg.color = new Color(theme.surface.r, theme.surface.g, theme.surface.b, 0.96f);
                var sh = bg.GetComponent<Shadow>();
                if (sh == null) sh = bg.gameObject.AddComponent<Shadow>();
                sh.effectColor = new Color(0f, 0f, 0f, 0.25f);
                sh.effectDistance = new Vector2(0f, -4f);
                EditorUtility.SetDirty(bg);
            }
            else
            {
                Debug.LogWarning("[Dashboard] No se encontro el Image de fondo del panel de objetivos.");
            }

            // Colores de texto legibles sobre fondo claro.
            var so = new SerializedObject(oc);
            SetColor(so, "pendingColor", theme.textPrimary);
            SetColor(so, "completedColor", theme.success);
            SetColor(so, "failedColor", theme.danger);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(oc);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Dashboard] Panel de objetivos restilado (fondo crema + colores legibles). " +
                      "OJO: las filas se regeneran en runtime con estos colores. Guardar (Ctrl+S).");
        }

        private static void SetColor(SerializedObject so, string prop, Color c)
        {
            var p = so.FindProperty(prop);
            if (p != null) p.colorValue = c;
        }

        // ============================================================
        //   Objetivos con scroll: header fijo + viewport recortado + ScrollRect
        // ============================================================

        [MenuItem("SafeDriver/UI/10. Objetivos con scroll (header fijo + scroll)")]
        public static void ScrollObjectives()
        {
            var theme = UIComposer.LoadTheme();
            if (theme == null) { Debug.LogError("[Dashboard] No se encontro EduTheme."); return; }

            var oc = Object.FindFirstObjectByType<ObjectivesController>(FindObjectsInactive.Include);
            if (oc == null) { Debug.LogWarning("[Dashboard] No se encontro ObjectivesController."); return; }

            var list = oc.GetComponent<RectTransform>();
            var panel = FindBackgroundPanel(oc);   // el Canvas/Image de fondo (ObjectivesPanel)
            if (panel == null) { Debug.LogWarning("[Dashboard] No se encontro el panel de fondo."); return; }

            float headerH = 54f;
            float gap = 14f;   // separacion entre el header y el area de tareas

            // Si ya hay Viewport, solo re-ajustar header + tope del viewport (no re-estructurar).
            var existingVp = panel.Find("Viewport") as RectTransform;
            if (existingVp != null)
            {
                existingVp.offsetMin = new Vector2(8f, 8f);
                existingVp.offsetMax = new Vector2(-20f, -(headerH + gap));  // -20 deja lugar al scrollbar
                var existingHeader = panel.Find("Header") as RectTransform;
                if (existingHeader != null)
                {
                    existingHeader.sizeDelta = new Vector2(-16f, headerH);
                    existingHeader.anchoredPosition = new Vector2(0f, -6f);
                }
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log("[Dashboard] Viewport del panel de objetivos bajado para no tapar el titulo.");
                return;
            }

            // 1. Header fijo arriba (titulo). El ObjectivesController deja de generar su titulo.
            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(CanvasRenderer));
            headerGo.transform.SetParent(panel, false);
            var hrt = headerGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f); hrt.anchorMax = new Vector2(1f, 1f);
            hrt.pivot = new Vector2(0.5f, 1f);
            hrt.anchoredPosition = new Vector2(0f, -6f);
            hrt.sizeDelta = new Vector2(-16f, headerH);
            var htmp = headerGo.AddComponent<TMPro.TextMeshProUGUI>();
            htmp.text = "OBJETIVOS";
            htmp.alignment = TMPro.TextAlignmentOptions.Center;
            UIThemeUtil.StyleText(htmp, theme, UIThemeUtil.TextKind.Heading);
            htmp.color = theme.textPrimary;
            htmp.enableAutoSizing = true; htmp.fontSizeMax = 26f; htmp.fontSizeMin = 12f;

            // 2. Viewport (debajo del header) con mascara que recorta.
            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer));
            viewportGo.transform.SetParent(panel, false);
            var vrt = viewportGo.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0f, 0f); vrt.anchorMax = new Vector2(1f, 1f);
            vrt.pivot = new Vector2(0.5f, 1f);
            vrt.offsetMin = new Vector2(8f, 8f);
            vrt.offsetMax = new Vector2(-8f, -(headerH + gap));
            var vimg = viewportGo.AddComponent<Image>();
            vimg.color = new Color(1f, 1f, 1f, 0.001f); // casi invisible; necesario para el mask/raycast
            viewportGo.AddComponent<RectMask2D>();

            // 3. El List (con el ObjectivesController) pasa a ser el Content, dentro del Viewport.
            list.SetParent(vrt, false);
            list.anchorMin = new Vector2(0f, 1f); list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.anchoredPosition = Vector2.zero;
            // El ContentSizeFitter (vertical) del List define su alto segun las filas; el ScrollRect lo mueve.

            // 4. ScrollRect en el panel.
            var scroll = panel.GetComponent<ScrollRect>();
            if (scroll == null) scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.content = list;
            scroll.viewport = vrt;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            // 5. El ObjectivesController ya no genera su propio titulo (lo da el header fijo).
            var so = new SerializedObject(oc);
            var titleProp = so.FindProperty("titleText");
            if (titleProp != null) { titleProp.stringValue = ""; so.ApplyModifiedProperties(); }
            EditorUtility.SetDirty(oc);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Dashboard] Objetivos con scroll: header fijo 'OBJETIVOS' + viewport recortado + ScrollRect. Guardar (Ctrl+S).");
        }

        // ============================================================
        //   Panel de fondo detras del cluster de displays (limite/timer/score)
        // ============================================================

        [MenuItem("SafeDriver/UI/12. Cluster de displays en un canvas alineado")]
        public static void ClusterPanel()
        {
            var theme = UIComposer.LoadTheme();
            if (theme == null) { Debug.LogError("[Dashboard] No se encontro EduTheme."); return; }

            var oldLimit = GameObject.Find("SpeedLimitSign");
            var oldTimer = GameObject.Find("TimerDisplay");
            var oldScore = GameObject.Find("ScoreDisplay");
            var prevCluster = GameObject.Find("DashboardCluster");
            // El parent correcto es el interior del auto (asi el cluster viaja con el vehiculo).
            // Lo derivamos de los displays viejos o del cluster previo; si ya no existen (re-ejecucion),
            // caemos al interior por nombre. OJO: los displays viejos se borran mas abajo, asi que hay
            // que capturar el parent ANTES de destruirlos.
            Transform parent = oldScore != null ? oldScore.transform.parent
                             : (oldLimit != null ? oldLimit.transform.parent
                             : (oldTimer != null ? oldTimer.transform.parent
                             : (prevCluster != null ? prevCluster.transform.parent : null)));
            if (parent == null)
            {
                var interior = GameObject.Find("SafeDriver_Exterior_v1/SafeDriver_Interior_v1");
                if (interior != null) parent = interior.transform;
                else Debug.LogWarning("[Dashboard] No se encontro el interior del auto; el cluster quedara en root y no seguira al vehiculo.");
            }

            // Limpiar versiones previas.
            var prevPanel = GameObject.Find("ClusterPanel"); if (prevPanel != null) Object.DestroyImmediate(prevPanel);
            if (prevCluster != null) Object.DestroyImmediate(prevCluster);

            // Canvas worldspace que agrupa los 3 displays, coplanar (un solo plano inclinado).
            var go = new GameObject("DashboardCluster", typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                var cso = new SerializedObject(canvas);
                cso.FindProperty("m_Camera").objectReferenceValue = centerEye.GetComponent<Camera>();
                cso.ApplyModifiedProperties();
            }
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(240f, 320f);
            rt.localScale = Vector3.one * 0.001f;
            rt.position = new Vector3(0.162f, 1.17f, 0.40f);
            rt.rotation = Quaternion.Euler(15f, 0f, 0f);

            var bg = go.AddComponent<Image>();
            if (theme.roundedSprite != null) { bg.sprite = theme.roundedSprite; bg.type = Image.Type.Sliced; }
            bg.color = new Color(theme.surface.r, theme.surface.g, theme.surface.b, 0.94f);
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.25f);
            sh.effectDistance = new Vector2(0f, -4f);

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 14, 14);
            vlg.spacing = 6f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            // 3 filas alineadas (etiqueta + numero), coplanares.
            var limitTmp = MakeClusterRow(go.transform, theme, "Limite", "LIMITE", "40", theme.danger);
            var timerTmp = MakeClusterRow(go.transform, theme, "Timer",  "TIEMPO", "02:00", theme.textPrimary);
            var scoreTmp = MakeClusterRow(go.transform, theme, "Score",  "PUNTOS", "1000", theme.brand);

            // Re-apuntar las referencias del HUD y del Timer a los nuevos textos UGUI.
            var hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud != null)
            {
                var hso = new SerializedObject(hud);
                hso.FindProperty("speedLimitSign").objectReferenceValue = limitTmp;
                hso.FindProperty("scoreDisplay").objectReferenceValue = scoreTmp;
                hso.ApplyModifiedProperties();
                EditorUtility.SetDirty(hud);
            }
            var timer = Object.FindFirstObjectByType<LevelTimer>(FindObjectsInactive.Include);
            if (timer != null)
            {
                var tso = new SerializedObject(timer);
                var dp = tso.FindProperty("display");
                if (dp != null) dp.objectReferenceValue = timerTmp;
                // El timer pinta el numero con normalColor en runtime; el default es blanco y se
                // pierde sobre el panel crema. Lo bajamos a textPrimary para que contraste.
                var nc = tso.FindProperty("normalColor");
                if (nc != null) nc.colorValue = theme.textPrimary;
                tso.ApplyModifiedProperties();
                EditorUtility.SetDirty(timer);
            }

            // Borrar los TMP 3D viejos (ya reemplazados).
            if (oldLimit != null) Object.DestroyImmediate(oldLimit);
            if (oldTimer != null) Object.DestroyImmediate(oldTimer);
            if (oldScore != null) Object.DestroyImmediate(oldScore);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Dashboard] Cluster en canvas alineado: LIMITE/TIEMPO/PUNTOS en un solo panel coplanar. Guardar (Ctrl+S).");
        }

        // Una fila del cluster: etiqueta chica arriba + numero grande, en un contenedor vertical.
        // Devuelve el TMP del numero (lo que actualizan HUD/Timer en runtime).
        private static TMP_Text MakeClusterRow(Transform parent, SafeDriver.UI.UITheme theme, string name, string label, string value, Color valueColor)
        {
            var row = new GameObject(name, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 74f; le.flexibleHeight = 0f;
            var vlg = row.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 0f; vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true; vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            var lblGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer));
            lblGo.transform.SetParent(row.transform, false);
            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.text = label;
            lbl.alignment = TextAlignmentOptions.Center;
            if (theme.bodyFont != null) lbl.font = theme.bodyFont;
            lbl.fontSize = 14f; lbl.color = theme.textSecondary; lbl.fontStyle = FontStyles.Bold;

            var valGo = new GameObject("Value", typeof(RectTransform), typeof(CanvasRenderer));
            valGo.transform.SetParent(row.transform, false);
            var val = valGo.AddComponent<TextMeshProUGUI>();
            val.text = value;
            val.alignment = TextAlignmentOptions.Center;
            if (theme.titleFont != null) val.font = theme.titleFont;
            val.fontSize = 30f; val.color = valueColor;
            return val;
        }

        // El fondo es el Canvas/Image que contiene al List del ObjectivesController.
        private static RectTransform FindBackgroundPanel(ObjectivesController oc)
        {
            var canvas = oc.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.GetComponent<Image>() != null)
                return canvas.GetComponent<RectTransform>();
            var img = oc.GetComponentInParent<Image>();
            return img != null ? img.rectTransform : null;
        }

        // ============================================================
        //   Objetivos: poke (para arrastrar) + scrollbar visible
        // ============================================================

        [MenuItem("SafeDriver/UI/11. Objetivos: poke + scrollbar")]
        public static void PokeAndScrollbar()
        {
            var theme = UIComposer.LoadTheme();
            var oc = Object.FindFirstObjectByType<ObjectivesController>(FindObjectsInactive.Include);
            if (oc == null) { Debug.LogWarning("[Dashboard] No se encontro ObjectivesController."); return; }
            var panel = FindBackgroundPanel(oc);
            var scroll = panel != null ? panel.GetComponent<ScrollRect>() : null;
            var viewport = panel != null ? panel.Find("Viewport") as RectTransform : null;
            if (panel == null || scroll == null || viewport == null)
            {
                Debug.LogWarning("[Dashboard] Corre primero '10. Objetivos con scroll'.");
                return;
            }

            // 1. Poke: duplicar el ISDK_PokeCanvasInteraction del VolumeSlider sobre este panel.
            if (panel.Find("ISDK_PokeCanvasInteraction") == null)
            {
                GameObject template = null;
                foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (go.name == "ISDK_PokeCanvasInteraction" && go.GetComponent<PokeInteractable>() != null
                        && go.transform.parent != null && go.transform.parent.name != "ObjectivesPanel")
                    { template = go; break; }

                if (template != null)
                {
                    var copy = Object.Instantiate(template, panel);
                    copy.name = "ISDK_PokeCanvasInteraction";
                    var crt = copy.GetComponent<RectTransform>();
                    crt.localPosition = Vector3.zero; crt.localRotation = Quaternion.identity; crt.localScale = Vector3.one;
                    crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
                    crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
                    copy.transform.SetAsFirstSibling(); // detras del contenido
                    var pc = copy.GetComponent<PointableCanvas>();
                    if (pc != null)
                    {
                        var pso = new SerializedObject(pc);
                        var pcanvas = pso.FindProperty("_canvas");
                        if (pcanvas != null) { pcanvas.objectReferenceValue = panel.GetComponent<Canvas>(); pso.ApplyModifiedProperties(); }
                    }
                    Debug.Log("[Dashboard] Poke agregado al panel de objetivos.");
                }
                else Debug.LogWarning("[Dashboard] No se encontro template de poke (VolumeSlider).");
            }

            // 2. Scrollbar vertical visible a la derecha del viewport.
            if (panel.Find("ScrollbarV") == null)
            {
                var sbGo = new GameObject("ScrollbarV", typeof(RectTransform), typeof(CanvasRenderer));
                sbGo.transform.SetParent(panel, false);
                var sbrt = sbGo.GetComponent<RectTransform>();
                sbrt.anchorMin = new Vector2(1f, 0f); sbrt.anchorMax = new Vector2(1f, 1f);
                sbrt.pivot = new Vector2(1f, 0.5f);
                sbrt.sizeDelta = new Vector2(12f, -16f);
                sbrt.anchoredPosition = new Vector2(-4f, 0f);
                var sbBg = sbGo.AddComponent<Image>();
                if (theme != null && theme.roundedSprite != null) { sbBg.sprite = theme.roundedSprite; sbBg.type = Image.Type.Sliced; }
                sbBg.color = theme != null ? new Color(theme.border.r, theme.border.g, theme.border.b, 0.6f) : new Color(0.8f,0.8f,0.8f,0.6f);
                var sb = sbGo.AddComponent<Scrollbar>();
                sb.direction = Scrollbar.Direction.BottomToTop;

                var area = new GameObject("Sliding Area", typeof(RectTransform));
                area.transform.SetParent(sbGo.transform, false);
                var art = area.GetComponent<RectTransform>();
                art.anchorMin = Vector2.zero; art.anchorMax = Vector2.one;
                art.offsetMin = new Vector2(1f, 1f); art.offsetMax = new Vector2(-1f, -1f);

                var handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer));
                handle.transform.SetParent(area.transform, false);
                var hrt = handle.GetComponent<RectTransform>();
                hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
                hrt.offsetMin = Vector2.zero; hrt.offsetMax = Vector2.zero;
                var hImg = handle.AddComponent<Image>();
                if (theme != null && theme.roundedSprite != null) { hImg.sprite = theme.roundedSprite; hImg.type = Image.Type.Sliced; }
                hImg.color = theme != null ? theme.brand : new Color(0.3f,0.55f,0.9f,1f);

                sb.targetGraphic = hImg;
                sb.handleRect = hrt;

                scroll.verticalScrollbar = sb;
                Debug.Log("[Dashboard] Scrollbar vertical agregado.");
            }

            // CLAVE: AutoHide (NO ...AndExpandViewport) para que el ScrollRect no redimensione
            // el viewport y respete su posicion debajo del header.
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Dashboard] Objetivos: poke + scrollbar listos. Guardar (Ctrl+S).");
        }
    }
}
