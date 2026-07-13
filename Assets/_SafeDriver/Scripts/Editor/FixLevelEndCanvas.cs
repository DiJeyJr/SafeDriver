using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Arregla el LevelEndCanvas de la escena abierta:
    /// 1. Lo sube (los botones quedaban metidos en el capo del auto).
    /// 2. Le agrega interaccion por RAY (solo tenia poke y esta lejos para el brazo):
    ///    RayInteractable + superficie con collider, apuntando al PointableCanvas existente.
    /// Re-entrante.
    /// </summary>
    public static class FixLevelEndCanvas
    {
        [MenuItem("SafeDriver/UI/Arreglar LevelEndCanvas (subir + ray)")]
        public static void Run()
        {
            var panel = Object.FindFirstObjectByType<LevelEndPanel>(FindObjectsInactive.Include);
            if (panel == null) { Debug.LogError("[LevelEnd] No hay LevelEndPanel en la escena."); return; }
            var canvasGo = panel.gameObject;

            // 1. Subirlo: quedaba a la altura del capo.
            var t = canvasGo.transform;
            var lp = t.localPosition;
            if (lp.y < 0.3f) t.localPosition = new Vector3(lp.x, 0.35f, lp.z);

            // 2. Ray: reutiliza el PointableCanvas que ya usa el poke.
            var pointable = canvasGo.GetComponentInChildren<PointableCanvas>(true);
            if (pointable == null)
            {
                Debug.LogError("[LevelEnd] No se encontro PointableCanvas en el canvas — no puedo wirear el ray.");
                return;
            }

            var prev = t.Find("ISDK_RayInteraction");
            if (prev != null) Object.DestroyImmediate(prev.gameObject);

            var rayGo = new GameObject("ISDK_RayInteraction");
            rayGo.transform.SetParent(t, false);

            // Superficie de hit: collider que cubre el canvas (compensa la escala 0.001).
            var surfGo = new GameObject("Surface");
            surfGo.transform.SetParent(rayGo.transform, false);
            surfGo.transform.localScale = Vector3.one * 1000f;
            var box = surfGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(0.62f, 0.5f, 0.02f);
            var surface = surfGo.AddComponent<ColliderSurface>();
            var soSurf = new SerializedObject(surface);
            soSurf.FindProperty("_collider").objectReferenceValue = box;
            soSurf.ApplyModifiedPropertiesWithoutUndo();

            var ray = rayGo.AddComponent<RayInteractable>();
            var soRay = new SerializedObject(ray);
            soRay.FindProperty("_pointableElement").objectReferenceValue = pointable;
            soRay.FindProperty("_surface").objectReferenceValue = surface;
            soRay.ApplyModifiedPropertiesWithoutUndo();

            // 3. Boton "Siguiente" entre Reintentar y Menu (3 en fila, achicados).
            var card = t.Find("Card");
            if (card != null)
            {
                var theme = UIComposer.LoadTheme();
                var retryRt = card.Find("RetryButton") as RectTransform;
                var menuRt = card.Find("MainMenuButton") as RectTransform;
                if (retryRt != null) { retryRt.anchoredPosition = new Vector2(-270f, 40f); retryRt.sizeDelta = new Vector2(250f, 88f); }
                if (menuRt != null) { menuRt.anchoredPosition = new Vector2(270f, 40f); menuRt.sizeDelta = new Vector2(250f, 88f); }

                var prevNext = card.Find("NextLevelButton");
                if (prevNext != null) Object.DestroyImmediate(prevNext.gameObject);

                var nextBtn = UIComposer.RoundButton(card, "NextLevelButton", "Siguiente", theme,
                    SafeDriver.UI.UIThemeUtil.Role.Brand, new Vector2(0f, 40f), new Vector2(250f, 88f));

                var soPanel = new SerializedObject(panel);
                soPanel.FindProperty("nextLevelButton").objectReferenceValue = nextBtn;
                soPanel.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[LevelEnd] No se encontro la Card — boton Siguiente no agregado.");

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[LevelEnd] Canvas subido, con ray y boton Siguiente. Guardar la escena.");
        }
    }
}
