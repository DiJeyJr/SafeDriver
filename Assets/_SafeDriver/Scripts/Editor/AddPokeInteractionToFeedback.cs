using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Oculus.Interaction;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Da interaccion por POKE (tocar con el control) a los canvas SafeFail y LevelEnd,
    /// replicando el "ISDK_PokeCanvasInteraction" del VolumeSliderCanvas (el patron que SI
    /// funciona en este proyecto). La interaccion de SafeDriver es por poke, no por ray:
    /// por eso nunca se ven rays y los botones no respondian con el setup de RayInteractable.
    ///
    /// Estructura replicada (del VolumeSliderCanvas):
    ///   Canvas
    ///   └─ ISDK_PokeCanvasInteraction  (PokeInteractable + PointableCanvas[_canvas=Canvas] + LayoutElement)
    ///       └─ Surface  (PlaneSurface + ClippedPlaneSurface + BoundsClipper + RectTransformBoundsClipperDriver)
    ///
    /// El RectTransform va con anchors 0..1 (cubre el canvas) y se adapta al tamanio destino.
    /// Limpia cualquier ISDK_RayInteraction previo y el PointableCanvas del canvas padre
    /// (sobrante del intento con ray).
    ///
    /// Disparar: SafeDriver/UI/7. Poke Interaction a SafeFail y LevelEnd.
    /// </summary>
    public static class AddPokeInteractionToFeedback
    {
        private const string PokeName = "ISDK_PokeCanvasInteraction";

        [MenuItem("SafeDriver/UI/7. Poke Interaction a SafeFail y LevelEnd")]
        public static void Run()
        {
            var template = FindTemplate();
            if (template == null)
            {
                Debug.LogError("[Poke] No se encontro el ISDK_PokeCanvasInteraction template (VolumeSliderCanvas) en la escena.");
                return;
            }

            AddTo("SafeFailCanvas", template);
            AddTo("LevelEndCanvas", template);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("[Poke] Poke interaction agregada a SafeFail y LevelEnd. Guardar (Ctrl+S).");
        }

        private static GameObject FindTemplate()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (go.name == PokeName && go.GetComponent<PokeInteractable>() != null)
                    return go;
            return null;
        }

        private static void AddTo(string canvasName, GameObject template)
        {
            var canvas = FindByName(canvasName);
            if (canvas == null) { Debug.LogWarning("[Poke] No se encontro " + canvasName); return; }

            bool wasActive = canvas.activeSelf;
            canvas.SetActive(true);

            // 1. Limpiar restos del intento con ray: ISDK_RayInteraction y el PointableCanvas del padre.
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
            {
                var c = canvas.transform.GetChild(i);
                if (c.name.Contains("RayInteraction") || c.name == PokeName)
                    Object.DestroyImmediate(c.gameObject);
            }
            var parentPc = canvas.GetComponent<PointableCanvas>();
            if (parentPc != null) Object.DestroyImmediate(parentPc);

            // 2. Duplicar el ISDK_PokeCanvasInteraction del VolumeSlider.
            var copy = Object.Instantiate(template, canvas.transform);
            copy.name = PokeName;
            var rt = copy.GetComponent<RectTransform>();
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;   // el original esta inclinado 35; aca el canvas no
            rt.localScale = Vector3.one;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // 3. Re-apuntar el PointableCanvas._canvas al Canvas destino (la ref externa no se remapea sola).
            var copyPc = copy.GetComponent<PointableCanvas>();
            if (copyPc != null)
            {
                var so = new SerializedObject(copyPc);
                var prop = so.FindProperty("_canvas");
                if (prop != null) { prop.objectReferenceValue = canvas.GetComponent<Canvas>(); so.ApplyModifiedProperties(); }
            }

            EditorUtility.SetDirty(copy);
            canvas.SetActive(wasActive);
            Debug.Log("[Poke] Poke interaction agregada a " + canvasName + ".");
        }

        private static GameObject FindByName(string name)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (c.name == name) return c.gameObject;
            return null;
        }
    }
}
