using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Construye el simbolo de integridad del auto (vista top-down con 8 zonas: frente, atras, 2
    /// laterales y 4 ruedas) que pasan de verde a rojo segun el daño, + un % total. Coloca uno en el
    /// tablero (live, sigue al auto) y otro en la pantalla de fin de nivel. Wirea el VehicleDamageSymbol.
    ///
    /// Las zonas se crean en el ORDEN EXACTO del enum IntegrityZone:
    ///   0 Front, 1 Rear, 2 LeftSide, 3 RightSide, 4 WheelFL, 5 WheelFR, 6 WheelRL, 7 WheelRR.
    /// Idempotente: borra simbolos previos y regenera.
    /// </summary>
    public static class CreateDamageSymbol
    {
        private const string ThemePath = "Assets/_SafeDriver/UI/EduTheme.asset";

        [MenuItem("SafeDriver/UI/Crear Simbolo de Integridad")]
        public static void Run()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);

            // -------- Tablero (canvas worldspace propio, hijo del interior del auto) --------
            var interior = GameObject.Find("SafeDriver_Exterior_v1/SafeDriver_Interior_v1");
            if (interior != null)
            {
                var prev = GameObject.Find("DamageSymbolCanvas");
                if (prev != null) Object.DestroyImmediate(prev);

                var go = new GameObject("DamageSymbolCanvas", typeof(RectTransform));
                go.transform.SetParent(interior.transform, false);
                var canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var centerEye = GameObject.Find("CenterEyeAnchor");
                if (centerEye != null)
                {
                    var cam = centerEye.GetComponent<Camera>();
                    var cso = new SerializedObject(canvas);
                    cso.FindProperty("m_Camera").objectReferenceValue = cam;
                    cso.ApplyModifiedProperties();
                }
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(190f, 260f);
                rt.localScale = Vector3.one * 0.001f;
                rt.position = new Vector3(-0.16f, 1.14f, 0.44f);
                rt.rotation = Quaternion.Euler(15f, 0f, 0f);

                BuildSymbol(go.transform, theme, "Tablero");
                Debug.Log("[Integridad] Simbolo del tablero creado (DamageSymbolCanvas, hijo del interior).");
            }
            else
            {
                Debug.LogWarning("[Integridad] No se encontro el interior del auto; no se creo el simbolo del tablero.");
            }

            // -------- Fin de nivel (dentro del canvas del LevelEndPanel) --------
            var levelEnd = Object.FindFirstObjectByType<LevelEndPanel>(FindObjectsInactive.Include);
            if (levelEnd != null)
            {
                var canvas = levelEnd.GetComponentInParent<Canvas>();
                Transform parent = canvas != null ? canvas.transform : levelEnd.transform;

                var prev = parent.Find("DamageSymbol_End");
                if (prev != null) Object.DestroyImmediate(prev.gameObject);

                var symbol = BuildSymbol(parent, theme, "End");
                var srt = symbol.GetComponent<RectTransform>();
                // A la derecha del panel; el usuario puede reubicarlo.
                srt.anchoredPosition = new Vector2(330f, -10f);
                srt.localScale = Vector3.one;
                Debug.Log("[Integridad] Simbolo de fin de nivel creado dentro del LevelEndPanel.");
            }
            else
            {
                Debug.LogWarning("[Integridad] No se encontro LevelEndPanel; no se creo el simbolo de fin de nivel.");
            }

            EditorSceneManager_MarkDirty();
        }

        // Construye el simbolo bajo 'parent' y devuelve su GameObject raiz (con VehicleDamageSymbol wireado).
        private static GameObject BuildSymbol(Transform parent, UITheme theme, string tag)
        {
            Color bodyCol   = new Color(0.24f, 0.26f, 0.30f, 1f);
            Color zoneGreen = new Color(0.30f, 0.78f, 0.36f, 1f);
            Color textCol   = theme != null ? theme.textPrimary : new Color(0.95f, 0.96f, 0.98f, 1f);
            Sprite rounded  = theme != null ? theme.roundedSprite : null;

            var root = new GameObject("DamageSymbol_" + tag, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            var rrt = root.GetComponent<RectTransform>();
            rrt.sizeDelta = new Vector2(170f, 240f);
            rrt.anchoredPosition = Vector2.zero;
            rrt.localScale = Vector3.one;

            // Titulo
            MakeText(root.transform, "Titulo", new Vector2(0f, 106f), new Vector2(170f, 26f), "INTEGRIDAD",
                     theme, 16f, textCol);

            // Carroceria (silueta)
            MakeImage(root.transform, "Body", new Vector2(0f, -6f), new Vector2(120f, 200f), bodyCol, rounded);

            // 8 zonas EN ORDEN del enum IntegrityZone.
            var zones = new Image[8];
            zones[0] = MakeImage(root.transform, "Z_Front",   new Vector2(0f,  72f),  new Vector2(92f, 30f), zoneGreen, rounded);
            zones[1] = MakeImage(root.transform, "Z_Rear",    new Vector2(0f, -84f),  new Vector2(92f, 30f), zoneGreen, rounded);
            zones[2] = MakeImage(root.transform, "Z_Left",    new Vector2(-44f, -6f), new Vector2(24f, 92f), zoneGreen, rounded);
            zones[3] = MakeImage(root.transform, "Z_Right",   new Vector2(44f, -6f),  new Vector2(24f, 92f), zoneGreen, rounded);
            zones[4] = MakeImage(root.transform, "Z_WheelFL", new Vector2(-52f, 48f), new Vector2(20f, 32f), zoneGreen, rounded);
            zones[5] = MakeImage(root.transform, "Z_WheelFR", new Vector2(52f,  48f), new Vector2(20f, 32f), zoneGreen, rounded);
            zones[6] = MakeImage(root.transform, "Z_WheelRL", new Vector2(-52f,-62f), new Vector2(20f, 32f), zoneGreen, rounded);
            zones[7] = MakeImage(root.transform, "Z_WheelRR", new Vector2(52f, -62f), new Vector2(20f, 32f), zoneGreen, rounded);

            // % total en el centro
            var pct = MakeText(root.transform, "Percent", new Vector2(0f, -6f), new Vector2(86f, 54f), "100%",
                               theme, 38f, textCol);

            var symbol = root.AddComponent<VehicleDamageSymbol>();
            var so = new SerializedObject(symbol);
            var arr = so.FindProperty("zoneImages");
            arr.arraySize = 8;
            for (int i = 0; i < 8; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = zones[i];
            so.FindProperty("percentText").objectReferenceValue = pct;
            so.ApplyModifiedProperties();

            return root;
        }

        private static Image MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.color = color;
            if (sprite != null) { img.sprite = sprite; img.type = Image.Type.Sliced; }
            return img;
        }

        private static TMP_Text MakeText(Transform parent, string name, Vector2 pos, Vector2 size, string text,
                                         UITheme theme, float fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = color;
            if (theme != null && theme.titleFont != null) tmp.font = theme.titleFont;
            return tmp;
        }

        private static void EditorSceneManager_MarkDirty()
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        }
    }
}
