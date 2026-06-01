using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Helpers de composicion para construir pantallas con craft (no apilado): card madre
    /// con sombra, banda de header de color, tarjetas internas, botones redondos con sombra,
    /// textos con jerarquia. Todos usan el UITheme. Compartido por los builders de pantallas.
    /// </summary>
    public static class UIComposer
    {
        public const string ThemePath = "Assets/_SafeDriver/UI/EduTheme.asset";

        public static UITheme LoadTheme() => AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);

        /// <summary>Fondo dim semi-transparente a todo el canvas (para overlays como SafeFail/LevelEnd).</summary>
        public static void DimBackground(Image canvasImg, float alpha = 0.35f)
        {
            if (canvasImg == null) return;
            canvasImg.sprite = null;
            canvasImg.type = Image.Type.Simple;
            canvasImg.color = new Color(0.08f, 0.10f, 0.16f, alpha);
        }

        /// <summary>Card centrada (superficie clara, redondeada, con sombra). Devuelve su RectTransform.</summary>
        public static RectTransform Card(Transform parent, UITheme t, Vector2 size, string name = "Card")
        {
            var go = NewUI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            Slice(img, t); img.color = t.surface;
            Shadow(img, new Color(0f, 0f, 0f, 0.28f), new Vector2(0f, -8f));
            return rt;
        }

        /// <summary>Banda de header de color pegada al tope del padre (esquinas redondeadas).</summary>
        public static RectTransform HeaderBand(Transform parent, UITheme t, Color color, float height, string name = "Header")
        {
            var go = NewUI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
            var img = go.AddComponent<Image>();
            Slice(img, t); img.color = color;
            return rt;
        }

        /// <summary>Panel interno (tarjeta diferenciada con tint) dentro de la card.</summary>
        public static RectTransform InnerCard(Transform parent, UITheme t, Vector2 anchorMin, Vector2 anchorMax, Color tint, string name = "Inner")
        {
            var go = NewUI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            Slice(img, t); img.color = tint;
            return rt;
        }

        /// <summary>Mezcla un color con blanco para tonos pastel suaves (fondos de tarjeta).</summary>
        public static Color Pastel(Color c, float amount = 0.82f) => Color.Lerp(c, Color.white, amount);

        public static TextMeshProUGUI Text(Transform parent, string content, UITheme t, UIThemeUtil.TextKind kind,
            Vector2 anchorMin, Vector2 anchorMax, TextAlignmentOptions align = TextAlignmentOptions.Center, string name = "Text")
        {
            var go = NewUI(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.richText = true;
            tmp.alignment = align;
            UIThemeUtil.StyleText(tmp, t, kind);
            return tmp;
        }

        /// <summary>Boton redondo con sombra y label. anchoredPosition relativa al centro-abajo del padre.</summary>
        public static Button RoundButton(Transform parent, string objName, string label, UITheme t,
            UIThemeUtil.Role role, Vector2 anchored, Vector2 size)
        {
            var go = NewUI(objName, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = size;

            var img = go.AddComponent<Image>();
            Slice(img, t);
            Color c = UIThemeUtil.ColorFor(t, role);
            img.color = c;
            Shadow(img, new Color(0f, 0f, 0f, 0.30f), new Vector2(0f, -5f));

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var cols = btn.colors;
            cols.normalColor = Color.white;
            cols.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cols.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            cols.fadeDuration = 0.08f;
            btn.colors = cols;

            var lbl = Text(go.transform, label, t, UIThemeUtil.TextKind.ButtonLabel,
                Vector2.zero, Vector2.one, TextAlignmentOptions.Center, "Label");
            lbl.color = t.textOnAccent;
            return btn;
        }

        // ---- internos ----

        private static GameObject NewUI(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Slice(Image img, UITheme t)
        {
            if (t.roundedSprite != null) { img.sprite = t.roundedSprite; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1f; }
        }

        private static void Shadow(Graphic g, Color color, Vector2 dist)
        {
            var sh = g.gameObject.GetComponent<Shadow>();
            if (sh == null) sh = g.gameObject.AddComponent<Shadow>();
            sh.effectColor = color;
            sh.effectDistance = dist;
        }
    }
}
