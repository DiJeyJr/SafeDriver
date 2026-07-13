using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using SafeDriver.Guidance;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Sistema de resaltado de elementos (esfera translucida pulsante):
    ///
    /// Item 1 — crea el material y el prefab ApproachHighlight (para resaltar carteles /
    /// semaforos / peatones la primera vez que el jugador se los cruza: arrastrar cerca
    /// del elemento, apuntar el target del HighlightMarker al elemento, ajustar el trigger).
    ///
    /// Item 2 — arma el tutorial de cabina en la escena abierta: resalta espejos ->
    /// freno de mano -> palanca de cambios -> guinie, de a uno; las palancas se apagan
    /// al agarrarlas, los espejos por tiempo.
    /// </summary>
    public static class CreateHighlightSystem
    {
        private const string MatDir = "Assets/_SafeDriver/Materials/Guidance";
        private const string MatPath = MatDir + "/HighlightSphere.mat";
        private const string PrefabDir = "Assets/_SafeDriver/Prefabs/Guidance";
        private const string PrefabPath = PrefabDir + "/ApproachHighlight.prefab";

        [MenuItem("SafeDriver/Guidance/1. Crear material y prefab de resaltado")]
        public static void BuildAssets()
        {
            EnsureFolders();
            var mat = EnsureMaterial();
            BuildApproachPrefab(mat);
            AssetDatabase.SaveAssets();
            Debug.Log("[Highlight] Material + prefab ApproachHighlight listos en " + PrefabDir);
        }

        [MenuItem("SafeDriver/Guidance/2. Armar tutorial de cabina (escena abierta)")]
        public static void BuildCabinTutorial()
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (mat == null) { BuildAssets(); mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath); }

            // Orden del tutorial (de lo mas cercano/notorio a lo demas): volante ->
            // espejo izquierdo -> espejo central -> espejo derecho -> freno de mano ->
            // palanca de cambios -> guinie. Volante y palancas se apagan al agarrar;
            // espejos al mirarlos (head-gaze, con timeout generoso de respaldo).
            var pasos = new List<HighlightMarker>();

            var volante = GameObject.Find("SteeringWheel");
            if (volante != null)
                pasos.Add(EnsureMarker(volante, mat, 0.5f, dismissSeconds: 0f, withGrab: true, withLook: false));
            else Debug.LogWarning("[Highlight] No se encontro 'SteeringWheel' en la escena.");

            foreach (var name in new[] { "Mirror_Left", "Mirror_Center", "Mirror_Right" })
            {
                var go = GameObject.Find(name);
                if (go == null) { Debug.LogWarning("[Highlight] No se encontro '" + name + "' en la escena."); continue; }
                // dismissSeconds 12 = respaldo por si el gaze no engancha; el look lo apaga antes.
                pasos.Add(EnsureMarker(go, mat, 0.3f, dismissSeconds: 12f, withGrab: false, withLook: true));
            }

            foreach (var name in new[] { "Handbrake", "GearShifter", "TurnSignalStalk" })
            {
                var go = GameObject.Find(name);
                if (go == null) { Debug.LogWarning("[Highlight] No se encontro '" + name + "' en la escena."); continue; }
                pasos.Add(EnsureMarker(go, mat, 0.4f, dismissSeconds: 0f, withGrab: true, withLook: false));
            }

            if (pasos.Count == 0) { Debug.LogError("[Highlight] No se encontro nada para resaltar."); return; }

            // Cartel "Chequeo de elementos en curso" arriba del volante (billboard, viaja con el auto).
            GameObject cartelGo = null;
            if (volante != null)
            {
                var prevCartel = GameObject.Find("TutorialSign");
                if (prevCartel != null) Object.DestroyImmediate(prevCartel);

                cartelGo = new GameObject("TutorialSign");
                cartelGo.transform.SetParent(volante.transform.parent, false);
                cartelGo.transform.position = volante.transform.position + Vector3.up * 0.28f;
                cartelGo.AddComponent<Billboard>();

                var bg = new GameObject("Fondo");
                bg.transform.SetParent(cartelGo.transform, false);
                var sr = bg.AddComponent<SpriteRenderer>();
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_SafeDriver/UI/RoundedRect.png");
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(0.4f, 0.12f);
                sr.color = new Color(1f, 0.992f, 0.969f, 0.92f);

                var txtGo = new GameObject("Texto");
                txtGo.transform.SetParent(cartelGo.transform, false);
                txtGo.transform.localPosition = new Vector3(0f, 0f, -0.005f);
                txtGo.transform.localScale = Vector3.one * 0.1f;
                var tmp = txtGo.AddComponent<TMPro.TextMeshPro>();
                tmp.text = "Chequeo de elementos\nen curso";
                tmp.fontSize = 1.6f;
                tmp.fontStyle = TMPro.FontStyles.Bold;
                tmp.alignment = TMPro.TextAlignmentOptions.Center;
                tmp.color = new Color(0.22f, 0.25f, 0.33f);
                tmp.rectTransform.sizeDelta = new Vector2(4f, 1.2f);

                cartelGo.SetActive(false); // lo prende la secuencia al arrancar
            }

            // Secuenciador (re-entrante: si ya existe se regenera).
            var prev = GameObject.Find("CabinTutorial");
            if (prev != null) Object.DestroyImmediate(prev);
            var seqGo = new GameObject("CabinTutorial");
            var seq = seqGo.AddComponent<HighlightSequence>();
            var so = new SerializedObject(seq);
            var arr = so.FindProperty("pasos");
            arr.arraySize = pasos.Count;
            for (int i = 0; i < pasos.Count; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = pasos[i];
            so.FindProperty("cartel").objectReferenceValue = cartelGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Highlight] Tutorial de cabina armado con " + pasos.Count + " pasos. Guardar la escena.");
        }

        /// <summary>
        /// Quita el tutorial de cabina de la escena abierta (secuenciador, cartel y todos
        /// los markers). Para niveles avanzados donde el chequeo de elementos ya no va.
        /// </summary>
        [MenuItem("SafeDriver/Guidance/3. Quitar tutorial de cabina (escena abierta)")]
        public static void RemoveCabinTutorial()
        {
            int borrados = 0;
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (t == null) continue;
                if (t.name == "CabinTutorial" || t.name == "TutorialSign" || t.name == "TutorialHighlight")
                {
                    Object.DestroyImmediate(t.gameObject);
                    borrados++;
                }
            }
            if (borrados > 0)
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Highlight] Tutorial de cabina quitado: " + borrados + " objetos borrados.");
        }

        // Crea (o reutiliza) el marker como hijo "TutorialHighlight" del elemento.
        private static HighlightMarker EnsureMarker(GameObject target, Material mat, float size, float dismissSeconds, bool withGrab, bool withLook = false)
        {
            var prev = target.transform.Find("TutorialHighlight");
            if (prev != null) Object.DestroyImmediate(prev.gameObject);

            var go = new GameObject("TutorialHighlight");
            go.transform.SetParent(target.transform, false);

            var marker = go.AddComponent<HighlightMarker>();
            var so = new SerializedObject(marker);
            so.FindProperty("haloMaterial").objectReferenceValue = mat;
            so.FindProperty("size").floatValue = size;
            so.FindProperty("showOnStart").boolValue = false; // lo maneja la secuencia
            so.FindProperty("dismissAfterSeconds").floatValue = dismissSeconds;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (withGrab) go.AddComponent<DismissOnGrab>();
            if (withLook) go.AddComponent<DismissOnLook>();
            return marker;
        }

        private static Material EnsureMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            if (existing != null) return existing;

            var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
            // Amarillo calido translucido (paleta warning del EduTheme).
            mat.SetColor("_BaseColor", new Color(0.98f, 0.73f, 0.25f, 0.4f));
            AssetDatabase.CreateAsset(mat, MatPath);
            return mat;
        }

        private static void BuildApproachPrefab(Material mat)
        {
            var root = new GameObject("ApproachHighlight");
            var box = root.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, 2f, 0f);
            box.size = new Vector3(14f, 4f, 20f); // zona de aproximacion generosa

            var marker = root.AddComponent<HighlightMarker>();
            var so = new SerializedObject(marker);
            so.FindProperty("haloMaterial").objectReferenceValue = mat;
            so.FindProperty("size").floatValue = 1.6f; // carteles / semaforos / peatones
            so.FindProperty("showOnStart").boolValue = false; // lo prende el approach
            so.ApplyModifiedPropertiesWithoutUndo();

            root.AddComponent<HighlightOnApproach>();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(MatDir))
                AssetDatabase.CreateFolder("Assets/_SafeDriver/Materials", "Guidance");
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets/_SafeDriver/Prefabs", "Guidance");
        }
    }
}
