using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using SafeDriver.Core;
using SafeDriver.Guidance;
using SafeDriver.Missions;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Genera el sistema de guiado por checkpoints: textura de flecha (procedural, estilo
    /// juego de carreras), materiales URP unlit transparentes (flecha + cilindro GTA) y el
    /// prefab CheckpointRoute listo para arrastrar a un nivel.
    ///
    /// Uso del level designer: arrastrar el prefab, mover/duplicar los hijos "Checkpoint_X"
    /// a lo largo del recorrido (uno por esquina o giro — las flechas van en linea recta
    /// entre checkpoints). El resto es automatico.
    ///
    /// Idempotente: la textura y materiales solo se crean si no existen; el prefab se regenera.
    /// </summary>
    public static class CreateCheckpointGuidance
    {
        private const string GuidanceDir = "Assets/_SafeDriver/Materials/Guidance";
        private const string TexPath = GuidanceDir + "/ArrowDecal.png";
        private const string ArrowMatPath = GuidanceDir + "/ArrowGuidance.mat";
        private const string CylMatPath = GuidanceDir + "/CylinderGuidance.mat";
        private const string PrefabDir = "Assets/_SafeDriver/Prefabs/Guidance";
        private const string PrefabPath = PrefabDir + "/CheckpointRoute.prefab";

        [MenuItem("SafeDriver/Guidance/Crear sistema de checkpoints (textura + materiales + prefab)")]
        public static void BuildAll()
        {
            EnsureFolders();
            var tex = EnsureArrowTexture();
            var arrowMat = EnsureMaterial(ArrowMatPath, tex, new Color(0.29f, 0.76f, 0.38f, 0.95f));
            var cylMat = EnsureMaterial(CylMatPath, null, new Color(0.18f, 0.56f, 0.9f, 0.25f));
            BuildPrefab(arrowMat, cylMat);

            AssetDatabase.SaveAssets();
            Debug.Log("[Guidance] Sistema de checkpoints listo: " + PrefabPath +
                      " — arrastrar al nivel y mover/duplicar los hijos Checkpoint_X por el recorrido.");
        }

        // Chevron de carreras: dos flechas apiladas apuntando hacia +V (arriba de la textura).
        private static Texture2D EnsureArrowTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
            if (existing != null) return existing;

            const int S = 256;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var clear = new Color(1f, 1f, 1f, 0f);
            var white = Color.white;

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                    tex.SetPixel(x, y, clear);

            // Dos chevrones (bandas en V invertida apuntando arriba), grosor 20% del alto.
            float half = S * 0.5f;
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = Mathf.Abs(x - half) / half;          // 0 centro .. 1 borde
                    float baseY = (1f - dx) * S * 0.35f;            // altura de la V segun x
                    float yf = y;
                    // chevron inferior y superior (offset 0 y 0.38*S)
                    for (int c = 0; c < 2; c++)
                    {
                        float y0 = baseY + c * S * 0.38f + S * 0.08f;
                        if (yf >= y0 && yf <= y0 + S * 0.16f && dx <= 0.85f)
                            tex.SetPixel(x, y, white);
                    }
                }
            }
            tex.Apply();

            File.WriteAllBytes(TexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexPath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(TexPath);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);
        }

        // Material URP Unlit transparente (la incantacion completa de keywords/blend).
        private static Material EnsureMaterial(string path, Texture2D tex, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader);
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", color);
            if (tex != null) mat.SetTexture("_BaseMap", tex);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static void BuildPrefab(Material arrowMat, Material cylMat)
        {
            var root = new GameObject("CheckpointRoute");
            var route = root.AddComponent<CheckpointRoute>();

            var so = new SerializedObject(route);
            so.FindProperty("arrowMaterial").objectReferenceValue = arrowMat;
            so.FindProperty("cylinderMaterial").objectReferenceValue = cylMat;
            so.ApplyModifiedProperties();

            // Mision de meta: al llegar al ultimo checkpoint, la ruta dispara ReachedGoal
            // y esta mision (registrada por el MissionKit) se completa.
            var kit = root.AddComponent<MissionKit>();
            var soKit = new SerializedObject(kit);
            soKit.FindProperty("missionTemplate").objectReferenceValue = EnsureMetaTemplate();
            soKit.ApplyModifiedProperties();

            // Tres checkpoints de muestra en linea; el designer los mueve/duplica.
            for (int i = 0; i < 3; i++)
            {
                var cp = new GameObject("Checkpoint_" + i);
                cp.transform.SetParent(root.transform, false);
                cp.transform.localPosition = new Vector3(0f, 0f, i * 25f);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        // Template de la mision de meta (solo si no existe; no pisa ajustes a mano).
        private static CountableMissionDefinition EnsureMetaTemplate()
        {
            const string path = "Assets/_SafeDriver/Missions/Templates/Kit_Meta.asset";
            var existing = AssetDatabase.LoadAssetAtPath<CountableMissionDefinition>(path);
            if (existing != null) return existing;

            if (!AssetDatabase.IsValidFolder("Assets/_SafeDriver/Missions/Templates"))
                AssetDatabase.CreateFolder("Assets/_SafeDriver/Missions", "Templates");

            var def = ScriptableObject.CreateInstance<CountableMissionDefinition>();
            def.missionId = "kit_meta";
            def.title = "Llegar a la meta";
            def.action = ActionType.ReachedGoal;
            def.requiredCount = 1;
            def.points = 10;
            AssetDatabase.CreateAsset(def, path);
            return def;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_SafeDriver/Materials"))
                AssetDatabase.CreateFolder("Assets/_SafeDriver", "Materials");
            if (!AssetDatabase.IsValidFolder(GuidanceDir))
                AssetDatabase.CreateFolder("Assets/_SafeDriver/Materials", "Guidance");
            if (!AssetDatabase.IsValidFolder("Assets/_SafeDriver/Prefabs"))
                AssetDatabase.CreateFolder("Assets/_SafeDriver", "Prefabs");
            if (!AssetDatabase.IsValidFolder(PrefabDir))
                AssetDatabase.CreateFolder("Assets/_SafeDriver/Prefabs", "Guidance");
        }
    }
}
