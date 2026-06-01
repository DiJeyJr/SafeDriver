using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Construye SafeFailCanvas y LevelEndCanvas con composicion (no apilado):
    /// fondo dim + card madre con sombra + banda de header de color + jerarquia de
    /// textos + tarjeta interna + botones redondos. Asigna los SerializeField de cada
    /// controller (que wirean sus botones por codigo en Awake).
    ///
    /// Disparar: SafeDriver/UI/Construir SafeFail y LevelEnd.
    /// </summary>
    public static class BuildFeedbackScreens
    {
        [MenuItem("SafeDriver/UI/Construir SafeFail y LevelEnd")]
        public static void Build()
        {
            var theme = UIComposer.LoadTheme();
            if (theme == null) { Debug.LogError("[BuildFeedback] No se encontro EduTheme."); return; }

            BuildSafeFail(theme);
            BuildLevelEnd(theme);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[BuildFeedback] SafeFail y LevelEnd construidos con composicion. Guardar (Ctrl+S).");
        }

        private static void BuildSafeFail(UITheme theme)
        {
            var canvasGo = FindCanvas("SafeFailCanvas");
            if (canvasGo == null) { Debug.LogWarning("[BuildFeedback] SafeFailCanvas no encontrado."); return; }
            var screen = canvasGo.GetComponent<SafeFailScreen>();

            ClearBuilt(canvasGo.transform);
            UIComposer.DimBackground(canvasGo.GetComponent<Image>(), 0.40f);

            var card = UIComposer.Card(canvasGo.transform, theme, new Vector2(820f, 600f));

            var header = UIComposer.HeaderBand(card, theme, theme.danger, 150f);
            var title = UIComposer.Text(header, "¡INFRACCION!", theme, UIThemeUtil.TextKind.Title,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 1f));
            title.color = theme.textOnAccent;
            title.enableAutoSizing = true; title.fontSizeMax = 56f; title.fontSizeMin = 28f;

            var desc = UIComposer.Text(card, "Descripcion de la infraccion.", theme, UIThemeUtil.TextKind.Body,
                new Vector2(0.08f, 0.46f), new Vector2(0.92f, 0.72f), TextAlignmentOptions.Top);

            var lawCard = UIComposer.InnerCard(card, theme, new Vector2(0.10f, 0.27f), new Vector2(0.90f, 0.42f),
                UIComposer.Pastel(theme.warning, 0.80f), "LawCard");
            var law = UIComposer.Text(lawCard, "Ley 24.449", theme, UIThemeUtil.TextKind.Secondary,
                new Vector2(0.06f, 0f), new Vector2(0.94f, 1f));
            law.fontStyle = FontStyles.Italic;

            var retry = UIComposer.RoundButton(card, "RetryButton", "Reintentar", theme, UIThemeUtil.Role.Success,
                new Vector2(-175f, 40f), new Vector2(320f, 96f));
            var menu = UIComposer.RoundButton(card, "MainMenuButton", "Menu", theme, UIThemeUtil.Role.Danger,
                new Vector2(175f, 40f), new Vector2(320f, 96f));

            if (screen != null)
            {
                var so = new SerializedObject(screen);
                so.FindProperty("safeFailCanvas").objectReferenceValue = canvasGo.GetComponent<Canvas>();
                so.FindProperty("titleText").objectReferenceValue = title;
                so.FindProperty("descriptionText").objectReferenceValue = desc;
                so.FindProperty("lawReferenceText").objectReferenceValue = law;
                so.FindProperty("retryButton").objectReferenceValue = retry;
                so.FindProperty("mainMenuButton").objectReferenceValue = menu;
                so.FindProperty("canvasGroup").objectReferenceValue = canvasGo.GetComponent<CanvasGroup>();
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(screen);
            }
        }

        private static void BuildLevelEnd(UITheme theme)
        {
            var canvasGo = FindCanvas("LevelEndCanvas");
            if (canvasGo == null) { Debug.LogWarning("[BuildFeedback] LevelEndCanvas no encontrado."); return; }
            var panel = canvasGo.GetComponent<LevelEndPanel>();

            bool wasActive = canvasGo.activeSelf;
            canvasGo.SetActive(true);

            ClearBuilt(canvasGo.transform);
            UIComposer.DimBackground(canvasGo.GetComponent<Image>(), 0.40f);

            var card = UIComposer.Card(canvasGo.transform, theme, new Vector2(820f, 620f));

            var header = UIComposer.HeaderBand(card, theme, theme.brand, 140f);
            var title = UIComposer.Text(header, "FIN DEL NIVEL", theme, UIThemeUtil.TextKind.Title,
                new Vector2(0.04f, 0f), new Vector2(0.96f, 1f));
            title.color = theme.textOnAccent;
            title.enableAutoSizing = true; title.fontSizeMax = 52f; title.fontSizeMin = 26f;

            // Estado (APROBADO/REPROBADO) como banner secundario.
            var status = UIComposer.Text(card, "APROBADO", theme, UIThemeUtil.TextKind.Heading,
                new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.74f));
            status.color = theme.success;

            // Score grande en una tarjeta interna.
            var scoreCard = UIComposer.InnerCard(card, theme, new Vector2(0.28f, 0.46f), new Vector2(0.72f, 0.60f),
                UIComposer.Pastel(theme.brand, 0.84f), "ScoreCard");
            var score = UIComposer.Text(scoreCard, "1000", theme, UIThemeUtil.TextKind.Title,
                new Vector2(0f, 0f), new Vector2(1f, 1f));
            score.color = theme.brand;
            score.enableAutoSizing = true; score.fontSizeMax = 48f; score.fontSizeMin = 24f;

            // Lista de infracciones.
            var list = UIComposer.Text(card, "", theme, UIThemeUtil.TextKind.Body,
                new Vector2(0.12f, 0.27f), new Vector2(0.88f, 0.44f), TextAlignmentOptions.TopLeft);
            list.color = theme.textSecondary;
            list.fontSize = 20f;

            var retry = UIComposer.RoundButton(card, "RetryButton", "Reintentar", theme, UIThemeUtil.Role.Success,
                new Vector2(-175f, 40f), new Vector2(320f, 96f));
            var menu = UIComposer.RoundButton(card, "MainMenuButton", "Menu", theme, UIThemeUtil.Role.Danger,
                new Vector2(175f, 40f), new Vector2(320f, 96f));

            if (panel != null)
            {
                var so = new SerializedObject(panel);
                so.FindProperty("rootPanel").objectReferenceValue = null;
                so.FindProperty("titleText").objectReferenceValue = title;
                so.FindProperty("scoreText").objectReferenceValue = score;
                so.FindProperty("statusText").objectReferenceValue = status;
                so.FindProperty("infractionsListText").objectReferenceValue = list;
                so.FindProperty("retryButton").objectReferenceValue = retry;
                so.FindProperty("mainMenuButton").objectReferenceValue = menu;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(panel);
            }

            canvasGo.SetActive(wasActive);
        }

        // ---- helpers ----

        private static GameObject FindCanvas(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == name) return c.gameObject;
            return null;
        }

        // Borra los hijos construidos (todo lo que NO sea el Pointable Item BB / ISDK interaction).
        private static void ClearBuilt(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var child = t.GetChild(i);
                string n = child.name.ToLowerInvariant();
                if (n.Contains("isdk") || n.Contains("interaction") || n.Contains("pointable") || n.Contains("ray"))
                    continue; // preservar el BB de interaccion
                Object.DestroyImmediate(child.gameObject);
            }
        }
    }
}
