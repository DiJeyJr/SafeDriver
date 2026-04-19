using UnityEditor;
using UnityEngine;

namespace SafeDriver.EditorTools
{
    internal static class MirrorAssetCreator
    {
        [MenuItem("SafeDriver/Create Mirror RenderTexture/Center")]
        public static void CreateCenter() => Create("MirrorCenter");

        [MenuItem("SafeDriver/Create Mirror RenderTexture/Left")]
        public static void CreateLeft() => Create("MirrorLeft");

        [MenuItem("SafeDriver/Create Mirror RenderTexture/Right")]
        public static void CreateRight() => Create("MirrorRight");

        private static void Create(string assetName)
        {
            string path = $"Assets/_SafeDriver/Materials/{assetName}.renderTexture";
            var rt = new RenderTexture(384, 192, 16, RenderTextureFormat.Default)
            {
                name = assetName,
                antiAliasing = 1,
                useMipMap = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            AssetDatabase.CreateAsset(rt, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MirrorAssetCreator] Created RenderTexture at {path}");
        }
    }
}
