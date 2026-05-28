using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Crea/actualiza el asset EduTheme y lo aplica de forma conservadora a la escena
    /// activa: cambia solo apariencia (colores, sprite redondeado, fuente, sombra) y
    /// NUNCA toca RectTransform (posiciones/tamanios), para no romper el layout VR ya
    /// ajustado. Los roles de boton se detectan por nombre del GameObject.
    /// </summary>
    public static class EduThemeSetup
    {
        private const string ThemePath = "Assets/_SafeDriver/UI/EduTheme.asset";
        private const string LiberationPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const string OswaldPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Oswald Bold SDF.asset";

        [MenuItem("SafeDriver/UI/1. Crear o actualizar EduTheme")]
        public static UITheme CreateTheme()
        {
            EnsureFolder("Assets/_SafeDriver/UI");

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            // Fuentes built-in: Oswald (titulos, presencia) + LiberationSans (cuerpo, legible).
            var oswald = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OswaldPath);
            var liberation = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(LiberationPath);
            if (oswald != null) theme.titleFont = oswald;
            if (liberation != null) theme.bodyFont = liberation;

            // Sprite redondeado 9-sliced built-in de Unity.
            theme.roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log($"[EduTheme] Asset listo en {ThemePath}. titleFont={(theme.titleFont != null ? theme.titleFont.name : "NULL")}, " +
                      $"bodyFont={(theme.bodyFont != null ? theme.bodyFont.name : "NULL")}, " +
                      $"roundedSprite={(theme.roundedSprite != null ? theme.roundedSprite.name : "NULL")}");
            return theme;
        }

        [MenuItem("SafeDriver/UI/2. Aplicar EduTheme a la seleccion")]
        public static void ApplyToSelection()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null) theme = CreateTheme();

            var roots = Selection.gameObjects;
            if (roots == null || roots.Length == 0)
            {
                Debug.LogWarning("[EduTheme] Selecciona el GameObject raiz de una pantalla de menu " +
                                 "(ej. _PauseMenu, LevelEndPanel) y volve a aplicar. Asi no se toca el HUD diegetico.");
                return;
            }

            int texts = 0, panels = 0, buttons = 0;
            foreach (var root in roots)
            {
                foreach (var img in root.GetComponentsInChildren<Image>(true))
                {
                    if (img.GetComponent<Button>() != null)
                    {
                        UIThemeUtil.StyleAsButton(img, theme, RoleFromName(img.name));
                        UIThemeUtil.AddSoftShadow(img, theme);
                        buttons++;
                    }
                    else
                    {
                        if (IsLargeBackground(img)) UIThemeUtil.StyleAsBackground(img, theme);
                        else                        UIThemeUtil.StyleAsPanel(img, theme);
                        panels++;
                    }
                    EditorUtility.SetDirty(img);
                }

                foreach (var txt in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    UIThemeUtil.StyleText(txt, theme, KindFromContext(txt));
                    EditorUtility.SetDirty(txt);
                    texts++;
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[EduTheme] Aplicado a la seleccion: {panels} paneles/fondos, {buttons} botones, {texts} textos. " +
                      "Revisar visualmente y guardar (Ctrl+S).");
        }

        // Rol de boton por nombre del GameObject (continuar/jugar = verde, salir = rojo, resto = azul).
        private static UIThemeUtil.Role RoleFromName(string name)
        {
            string n = name.ToLowerInvariant();
            if (n.Contains("continue") || n.Contains("continuar") || n.Contains("play") ||
                n.Contains("jugar") || n.Contains("retry") || n.Contains("reintentar") || n.Contains("next") || n.Contains("siguiente"))
                return UIThemeUtil.Role.Success;
            if (n.Contains("quit") || n.Contains("salir") || n.Contains("exit") || n.Contains("mainmenu") || n.Contains("menu"))
                return UIThemeUtil.Role.Danger;
            return UIThemeUtil.Role.Brand;
        }

        // Texto: titulo si el nombre lo indica o la fuente es grande; label de boton si es hijo de un Button.
        private static UIThemeUtil.TextKind KindFromContext(TextMeshProUGUI txt)
        {
            if (txt.GetComponentInParent<Button>() != null)
                return UIThemeUtil.TextKind.ButtonLabel;

            string n = txt.name.ToLowerInvariant();
            if (n.Contains("title") || n.Contains("titulo") || n.Contains("header") || txt.fontSize >= 34f)
                return UIThemeUtil.TextKind.Title;
            if (n.Contains("status") || n.Contains("score") || n.Contains("heading"))
                return UIThemeUtil.TextKind.Heading;
            return UIThemeUtil.TextKind.Body;
        }

        private static bool IsLargeBackground(Image img)
        {
            var rt = img.rectTransform;
            // Anchors estirados a todo el padre => fondo de pantalla.
            bool stretched = rt.anchorMin == Vector2.zero && rt.anchorMax == Vector2.one;
            return stretched;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int lastSlash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, lastSlash), path.Substring(lastSlash + 1));
        }
    }
}
