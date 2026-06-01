using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Aplica la tipografia del theme al HUD del auto (velocimetro, score, limite, timer),
    /// al panel de objetivos del tablero y al panel de avisos rapidos (infraccion/acierto).
    /// Solo cambia FUENTE y forma del panel de avisos; preserva color y tamanio de los
    /// displays del tablero para no romper su look de auto.
    ///
    /// Disparar: SafeDriver/UI/5. Aplicar tipografia al HUD del auto.
    /// </summary>
    public static class RestyleHUD
    {
        private const string ThemePath = "Assets/_SafeDriver/UI/EduTheme.asset";

        [MenuItem("SafeDriver/UI/5. Aplicar tipografia al HUD del auto")]
        public static void Run()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null) { Debug.LogError("[RestyleHUD] No se encontro EduTheme."); return; }
            var body = theme.bodyFont;
            if (body == null) { Debug.LogWarning("[RestyleHUD] El theme no tiene bodyFont."); }

            int changed = 0;

            // 1. HUD del tablero: cambiar fuente de todos los TextMeshPro 3D de la escena
            //    (displays diegeticos). Preserva color/tamanio.
            foreach (var t in Object.FindObjectsByType<TextMeshPro>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (body != null) { t.font = body; changed++; EditorUtility.SetDirty(t); }
            }

            // 2. Panel de avisos rapidos (notificationPanel) del HUDController: sprite redondeado.
            var hud = Object.FindFirstObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud != null)
            {
                var so = new SerializedObject(hud);
                var panelProp = so.FindProperty("notificationPanel");
                if (panelProp != null && panelProp.objectReferenceValue is GameObject panelGo)
                {
                    var img = panelGo.GetComponent<Image>();
                    if (img != null) { UIThemeUtil.StyleAsPanel(img, theme); changed++; EditorUtility.SetDirty(img); }
                    foreach (var tmp in panelGo.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (body != null) { tmp.font = body; changed++; EditorUtility.SetDirty(tmp); }
                    }
                }
            }

            // 3. Panel de objetivos del tablero: asignar la fuente al ObjectivesController
            //    (sus filas se generan en runtime usando ese campo).
            foreach (var oc in Object.FindObjectsByType<ObjectivesController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(oc);
                var fontProp = so.FindProperty("font");
                if (fontProp != null) { fontProp.objectReferenceValue = body; so.ApplyModifiedProperties(); changed++; EditorUtility.SetDirty(oc); }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"[RestyleHUD] Tipografia aplicada al HUD/tablero/avisos: {changed} elementos. Guardar (Ctrl+S).");
        }
    }
}
