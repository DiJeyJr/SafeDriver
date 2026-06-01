using System.IO;
using UnityEditor;
using UnityEngine;
using TMPro;
using SafeDriver.UI;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Genera los assets del theme infantil-redondeado y los asigna al EduTheme:
    ///   - TMP_FontAsset de Baloo 2 (titulos/botones) y Varela Round (cuerpo).
    ///   - Un sprite rounded-rect con esquinas bien redondas (radio generoso) 9-sliced.
    ///   - Ajusta la paleta a tonos mas vivos/amigables.
    ///
    /// Disparar: SafeDriver/UI/0. Generar assets del theme (fuentes + sprite).
    /// </summary>
    public static class GenerateThemeAssets
    {
        private const string Dir = "Assets/_SafeDriver/UI";
        private const string FontsDir = Dir + "/Fonts";
        private const string ThemePath = Dir + "/EduTheme.asset";

        [MenuItem("SafeDriver/UI/0. Generar assets del theme (fuentes + sprite)")]
        public static void Generate()
        {
            var titleFont = GenerateFontAsset(FontsDir + "/Baloo2-Variable.ttf", FontsDir + "/Baloo2 SDF.asset");
            var bodyFont  = GenerateFontAsset(FontsDir + "/VarelaRound-Regular.ttf", FontsDir + "/VarelaRound SDF.asset");
            var rounded   = GenerateRoundedSprite(Dir + "/RoundedRect.png", 64, 22);

            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null) { Debug.LogError("[ThemeAssets] No se encontro EduTheme."); return; }

            if (titleFont != null) theme.titleFont = titleFont;
            if (bodyFont != null)  theme.bodyFont  = bodyFont;
            if (rounded != null)   theme.roundedSprite = rounded;

            // Paleta mas viva/amigable manteniendo la semantica de senaletica.
            theme.backgroundBase = new Color(0.992f, 0.965f, 0.890f); // crema calido
            theme.surface        = new Color(1.000f, 0.992f, 0.969f); // casi blanco calido
            theme.brand          = new Color(0.180f, 0.560f, 0.900f); // azul vivo
            theme.success        = new Color(0.290f, 0.760f, 0.380f); // verde vivo
            theme.danger         = new Color(0.945f, 0.380f, 0.380f); // rojo coral mas suave/infantil
            theme.warning        = new Color(0.980f, 0.730f, 0.250f); // amarillo calido
            theme.textPrimary    = new Color(0.220f, 0.250f, 0.330f); // azul-gris oscuro, no negro
            theme.radiusSmall    = 22f;
            theme.radiusCard     = 28f;

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ThemeAssets] Listo. titleFont={(titleFont!=null?titleFont.name:"NULL")}, " +
                      $"bodyFont={(bodyFont!=null?bodyFont.name:"NULL")}, rounded={(rounded!=null?"OK":"NULL")}.");
        }

        // ============================================================
        //   TMP Font Asset
        // ============================================================

        private static TMP_FontAsset GenerateFontAsset(string ttfPath, string assetPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null) return existing;

            var font = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (font == null) { Debug.LogWarning("[ThemeAssets] No se encontro la fuente TTF: " + ttfPath); return null; }

            var fa = TMP_FontAsset.CreateFontAsset(font);
            if (fa == null) { Debug.LogWarning("[ThemeAssets] No se pudo crear TMP_FontAsset de " + ttfPath); return null; }

            AssetDatabase.CreateAsset(fa, assetPath);
            // El atlas y el material son sub-assets.
            if (fa.atlasTextures != null && fa.atlasTextures.Length > 0)
            {
                fa.atlasTextures[0].name = font.name + " Atlas";
                AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
            }
            if (fa.material != null)
            {
                fa.material.name = font.name + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            AssetDatabase.SaveAssets();
            return fa;
        }

        // ============================================================
        //   Sprite rounded-rect (esquinas redondas, 9-sliced)
        // ============================================================

        private static Sprite GenerateRoundedSprite(string pngPath, int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = InsideRoundedRect(x, y, size, size, radius);
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();

            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

            // Configurar el importer como Sprite con borde 9-slice = radio.
            var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            var border = new Vector4(radius, radius, radius, radius);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = border;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);

            // Forzar RGBA32 sin comprimir en Android: con la compresion automatica (PVRTC,
            // obsoleta) la textura no renderiza en el build de Quest (se ve "texto sin fondo").
            var android = new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                format = TextureImporterFormat.RGBA32,
                textureCompression = TextureImporterCompression.Uncompressed,
                maxTextureSize = 2048,
            };
            importer.SetPlatformTextureSettings(android);
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        }

        private static bool InsideRoundedRect(int x, int y, int w, int h, int r)
        {
            // Centros de las 4 esquinas redondeadas.
            int xMin = r, xMax = w - 1 - r, yMin = r, yMax = h - 1 - r;
            int cx = Mathf.Clamp(x, xMin, xMax);
            int cy = Mathf.Clamp(y, yMin, yMax);
            float dx = x - cx, dy = y - cy;
            return dx * dx + dy * dy <= (float)r * r;
        }
    }
}
