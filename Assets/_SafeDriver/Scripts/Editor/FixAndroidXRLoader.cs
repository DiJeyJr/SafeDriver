using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;

namespace SafeDriver.EditorTools
{
    /// <summary>
    /// Agrega el OculusLoader al XR Plug-in Management de Android.
    /// Sin esto, libOVRPlugin.so no se incluye en el APK y la app cuelga
    /// en loading con DllNotFoundException: 'OVRPlugin'.
    /// </summary>
    public static class FixAndroidXRLoader
    {
        [MenuItem("SafeDriver/Fix Android XR Loader (Oculus)")]
        public static void Apply()
        {
            var androidSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (androidSettings == null)
            {
                Debug.LogError("[FixAndroidXRLoader] No se encontraron XRGeneralSettings para Android.");
                return;
            }
            if (androidSettings.AssignedSettings == null)
            {
                Debug.LogError("[FixAndroidXRLoader] Android XRManagerSettings (AssignedSettings) es null.");
                return;
            }

            const string oculusLoaderType = "Unity.XR.Oculus.OculusLoader";

            bool ok = XRPackageMetadataStore.AssignLoader(
                androidSettings.AssignedSettings,
                oculusLoaderType,
                BuildTargetGroup.Android);

            if (ok)
            {
                EditorUtility.SetDirty(androidSettings);
                EditorUtility.SetDirty(androidSettings.AssignedSettings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[FixAndroidXRLoader] OculusLoader agregado a Android Providers. Loaders activos: " + androidSettings.AssignedSettings.activeLoaders.Count);
            }
            else
            {
                Debug.LogError("[FixAndroidXRLoader] AssignLoader devolvio false. Verificar que el paquete com.unity.xr.oculus este instalado.");
            }
        }

        [MenuItem("SafeDriver/Verify Android XR Loaders")]
        public static void Verify()
        {
            var androidSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (androidSettings == null || androidSettings.AssignedSettings == null)
            {
                Debug.LogWarning("[FixAndroidXRLoader] No hay settings de Android todavia.");
                return;
            }
            var loaders = androidSettings.AssignedSettings.activeLoaders;
            Debug.Log("[FixAndroidXRLoader] Android loaders count = " + loaders.Count);
            for (int i = 0; i < loaders.Count; i++)
            {
                Debug.Log("[FixAndroidXRLoader]   [" + i + "] " + (loaders[i] != null ? loaders[i].GetType().FullName : "<null>"));
            }
        }
    }
}
