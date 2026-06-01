using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Crea el canvas de la notificacion de aciertos (SuccessToast) apenas arriba del
    /// volante: una tarjeta verde redondeada con texto, que aparece al hacer algo bien y
    /// se desvanece sola. Usa el UITheme. Idempotente (si ya existe, lo reusa).
    ///
    /// Disparar: SafeDriver/UI/8. Crear Success Toast (arriba del volante).
    /// </summary>
    public static class CreateSuccessToast
    {
        private const string ToastName = "SuccessToastCanvas";

        [MenuItem("SafeDriver/UI/8. Crear Success Toast (arriba del volante)")]
        public static void Run()
        {
            var theme = UIComposer.LoadTheme();
            if (theme == null) { Debug.LogError("[Toast] No se encontro EduTheme."); return; }

            var existing = GameObject.Find(ToastName);
            if (existing != null) { Object.DestroyImmediate(existing); }

            var wheel = GameObject.Find("SteeringWheel");
            Transform parent = wheel != null && wheel.transform.parent != null ? wheel.transform.parent : null;

            // Canvas worldspace.
            var go = new GameObject(ToastName, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var centerEye = GameObject.Find("CenterEyeAnchor");
            if (centerEye != null)
            {
                var so = new SerializedObject(canvas);
                so.FindProperty("m_Camera").objectReferenceValue = centerEye.GetComponent<Camera>();
                so.ApplyModifiedProperties();
            }
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;     // solo visual, no intercepta interaccion
            cg.blocksRaycasts = false;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(440f, 150f);
            rt.localScale = Vector3.one * 0.001f;
            // Posicion: apenas arriba del volante, encarando al conductor.
            if (wheel != null)
                rt.position = wheel.transform.position + new Vector3(0f, 0.34f, 0.10f);
            else
                rt.position = new Vector3(-0.44f, 1.53f, 0.45f);
            rt.rotation = Quaternion.identity;

            // Tarjeta verde redondeada (fondo).
            var bg = go.AddComponent<Image>();
            if (theme.roundedSprite != null) { bg.sprite = theme.roundedSprite; bg.type = Image.Type.Sliced; }
            bg.color = theme.success;
            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.30f);
            shadow.effectDistance = new Vector2(0f, -4f);

            // Texto.
            var txtGo = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer));
            txtGo.transform.SetParent(go.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.06f, 0.08f); trt.anchorMax = new Vector2(0.94f, 0.92f);
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = "¡Muy bien!";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            UIThemeUtil.StyleText(tmp, theme, UIThemeUtil.TextKind.Heading);
            tmp.color = theme.textOnAccent;
            tmp.enableAutoSizing = true; tmp.fontSizeMax = 30f; tmp.fontSizeMin = 14f;

            // Componente.
            var toast = go.AddComponent<SuccessToast>();
            var tso = new SerializedObject(toast);
            tso.FindProperty("canvasGroup").objectReferenceValue = cg;
            tso.FindProperty("messageText").objectReferenceValue = tmp;
            tso.ApplyModifiedProperties();

            EditorUtility.SetDirty(go);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Toast] SuccessToastCanvas creado arriba del volante. Guardar (Ctrl+S).");
        }
    }
}
